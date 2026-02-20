using Palisades.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Palisades.Services
{
    public class CalDAVService
    {
        private readonly string _caldavUrl;
        private readonly HttpClient _httpClient;
        private readonly CalendarSerializer _serializer = new CalendarSerializer();

        /// <summary>
        /// Vérifie que l'URL utilise HTTPS lorsqu'elle est renseignée (Phase 3.3).
        /// </summary>
        private static void EnsureHttps(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("L'URL CalDAV doit utiliser HTTPS. Les connexions non sécurisées sont refusées.");
        }

        /// <param name="caldavUrl">Base CalDAV, ex. https://serveur/dav/email@domain/ (Zimbra OVH : tâches sous /Tasks)</param>
        public CalDAVService(string caldavUrl, string username, string password)
        {
            _caldavUrl = (caldavUrl ?? "").TrimEnd('/');
            EnsureHttps(_caldavUrl);

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username ?? "", password ?? ""),
                PreAuthenticate = true
            };
            _httpClient = new HttpClient(handler)
            {
                DefaultRequestHeaders =
                {
                    { "Depth", "1" },
                    { "User-Agent", "Palisades/1.0" }
                }
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/calendar"));
        }

        public async Task<List<CalDAVTaskList>> GetTaskListsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), _caldavUrl);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var taskLists = new List<CalDAVTaskList>();
                var xdoc = XDocument.Parse(body);
                var ns = XNamespace.Get("DAV:");
                var responses = xdoc.Descendants(ns + "response");
                foreach (var resp in responses)
                {
                    var href = resp.Descendants(ns + "href").FirstOrDefault()?.Value;
                    var displayName = resp.Descendants(ns + "displayname").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(href) && (href.Contains("/tasks/", StringComparison.OrdinalIgnoreCase) || href.Contains("/calendars/", StringComparison.OrdinalIgnoreCase)))
                    {
                        taskLists.Add(new CalDAVTaskList
                        {
                            Name = href.Split('/').Last(),
                            DisplayName = displayName ?? href.Split('/').Last(),
                            CalDAVUrl = href
                        });
                    }
                }
                return taskLists;
            }
            catch (Exception)
            {
                return new List<CalDAVTaskList>();
            }
        }

        public async Task<List<CalDAVTask>> GetTasksAsync(string taskListId)
        {
            try
            {
                var taskListUrl = $"{_caldavUrl}/{taskListId}";
                string requestBody = @"<?xml version='1.0' encoding='utf-8' ?>
<C:calendar-query xmlns:C=""urn:ietf:params:xml:ns:caldav"" xmlns:D=""DAV:"">
    <D:prop>
        <D:getetag/>
        <C:calendar-data/>
    </D:prop>
    <C:filter>
        <C:comp-filter name=""VCALENDAR"">
            <C:comp-filter name=""VTODO""/>
        </C:comp-filter>
    </C:filter>
</C:calendar-query>";

                var request = new HttpRequestMessage(new HttpMethod("REPORT"), taskListUrl);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/xml");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return ParseMultistatusCalendarData(body);
            }
            catch (Exception)
            {
                return new List<CalDAVTask>();
            }
        }

        /// <summary>Parse une réponse multistatus CalDAV et extrait les VTODO en CalDAVTask (Phase 4.1).</summary>
        private static List<CalDAVTask> ParseMultistatusCalendarData(string multistatusXml)
        {
            var tasks = new List<CalDAVTask>();
            var dav = XNamespace.Get("DAV:");
            var caldav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");
            try
            {
                var xdoc = XDocument.Parse(multistatusXml);
                foreach (var response in xdoc.Descendants(dav + "response"))
                {
                    var href = response.Descendants(dav + "href").FirstOrDefault()?.Value?.TrimEnd('/');
                    if (string.IsNullOrEmpty(href)) continue;
                    var caldavId = href.Contains('/') ? href.Substring(href.LastIndexOf('/') + 1) : href;
                    var propstat = response.Descendants(dav + "propstat").FirstOrDefault(p => p.Element(dav + "status")?.Value?.Contains("200") == true);
                    var prop = propstat?.Element(dav + "prop");
                    if (prop == null) continue;
                    var etagEl = prop.Element(dav + "getetag");
                    var etag = etagEl?.Value?.Trim('"') ?? "";
                    var calendarDataEl = prop.Element(caldav + "calendar-data");
                    var calendarData = calendarDataEl?.Value;
                    if (string.IsNullOrWhiteSpace(calendarData)) continue;
                    try
                    {
                        var calendar = Calendar.Load(calendarData);
                        foreach (var todo in calendar.Todos)
                        {
                            tasks.Add(MapTodoToCalDAVTask(todo, caldavId, etag));
                        }
                    }
                    catch { /* ignorer un bloc calendar invalide */ }
                }
            }
            catch { }
            return tasks;
        }

        private static CalDAVTask MapTodoToCalDAVTask(Todo todo, string caldavId, string etag)
        {
            var due = todo.Due?.Value;
            var completedDt = todo.Completed?.Value;
            var created = todo.Created?.Value ?? DateTime.UtcNow;
            var lastMod = todo.LastModified?.Value ?? DateTime.UtcNow;
            return new CalDAVTask(todo.Summary ?? "")
            {
                Description = todo.Description ?? "",
                DueDate = due,
                Completed = string.Equals(todo.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase),
                CompletedDate = completedDt,
                CreatedDate = created,
                LastModified = lastMod,
                CalDAVId = caldavId,
                Uid = todo.Uid ?? "",
                CalDAVEtag = etag
            };
        }

        public async Task<CalDAVTask> CreateTaskAsync(string taskListId, CalDAVTask task)
        {
            var calendar = new Calendar();
            var uid = string.IsNullOrEmpty(task.Uid) ? Guid.NewGuid().ToString() : task.Uid;
            var todo = new Todo
            {
                Summary = task.Title,
                Description = task.Description,
                Due = task.DueDate.HasValue ? new CalDateTime(task.DueDate.Value) : null,
                Status = task.Completed ? "COMPLETED" : "NEEDS-ACTION",
                Completed = task.Completed ? new CalDateTime(task.CompletedDate ?? DateTime.Now) : null,
                Created = new CalDateTime(task.CreatedDate),
                LastModified = new CalDateTime(task.LastModified),
                Uid = uid
            };
            calendar.Todos.Add(todo);
            var calendarData = _serializer.SerializeToString(calendar);

            var taskListUrl = $"{_caldavUrl}/{taskListId}";
            var taskFilename = $"{uid}.ics";
            var request = new HttpRequestMessage(HttpMethod.Put, $"{taskListUrl}/{taskFilename}");
            request.Content = new StringContent(calendarData, Encoding.UTF8, "text/calendar");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            task.CalDAVId = taskFilename;
            task.Uid = uid;
            task.CalDAVEtag = "created";
            return task;
        }

        public async Task UpdateTaskAsync(string taskListId, CalDAVTask task)
        {
            var calendar = new Calendar();
            var uid = !string.IsNullOrEmpty(task.Uid) ? task.Uid : (!string.IsNullOrEmpty(task.CalDAVId) ? Path.GetFileNameWithoutExtension(task.CalDAVId) : null) ?? Guid.NewGuid().ToString();
            var todo = new Todo
            {
                Summary = task.Title,
                Description = task.Description,
                Due = task.DueDate.HasValue ? new CalDateTime(task.DueDate.Value) : null,
                Status = task.Completed ? "COMPLETED" : "NEEDS-ACTION",
                Completed = task.Completed ? new CalDateTime(task.CompletedDate ?? DateTime.Now) : null,
                Created = new CalDateTime(task.CreatedDate),
                LastModified = new CalDateTime(DateTime.Now),
                Uid = uid
            };
            calendar.Todos.Add(todo);
            var calendarData = _serializer.SerializeToString(calendar);

            var taskListUrl = $"{_caldavUrl}/{taskListId}";
            var request = new HttpRequestMessage(HttpMethod.Put, $"{taskListUrl}/{task.CalDAVId}");
            request.Content = new StringContent(calendarData, Encoding.UTF8, "text/calendar");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            task.CalDAVEtag = "updated";
        }

        public async Task DeleteTaskAsync(string taskListId, string taskId)
        {
            var taskListUrl = $"{_caldavUrl}/{taskListId}";
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{taskListUrl}/{taskId}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Sync bidirectionnelle (Phase 4.3) : tâches distantes inconnues localement sont ajoutées, pas supprimées du serveur.</summary>
        public async Task<List<CalDAVTask>> SyncTasksAsync(string taskListId, List<CalDAVTask> localTasks)
        {
            var remoteTasks = await GetTasksAsync(taskListId);
            var merged = new List<CalDAVTask>();
            foreach (var local in localTasks)
            {
                var remote = remoteTasks.FirstOrDefault(r => r.CalDAVId == local.CalDAVId || (!string.IsNullOrEmpty(local.Uid) && r.Uid == local.Uid));
                if (remote == null)
                {
                    var created = await CreateTaskAsync(taskListId, local);
                    merged.Add(created);
                }
                else
                {
                    if (local.LastModified > remote.LastModified)
                        await UpdateTaskAsync(taskListId, local);
                    merged.Add(local.LastModified >= remote.LastModified ? local : remote);
                }
            }
            foreach (var remote in remoteTasks)
            {
                if (!merged.Any(m => m.CalDAVId == remote.CalDAVId || m.Uid == remote.Uid))
                    merged.Add(remote);
            }
            return merged;
        }
    }
}
