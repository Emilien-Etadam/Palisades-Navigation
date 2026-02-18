using Palisades.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using System.Net;
using System.IO;
using System.Xml.Linq;

namespace Palisades.Services
{
    public class CalDAVService
    {
        private readonly string _caldavUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly iCalendarSerializer _serializer = new iCalendarSerializer();
        
        public CalDAVService(string caldavUrl, string username, string password)
        {
            _caldavUrl = caldavUrl.TrimEnd('/');
            _username = username;
            _password = password;
        }
        
        public async Task<List<CalDAVTaskList>> GetTaskListsAsync()
        {
            try
            {
                var request = CreateCalDAVRequest("PROPFIND", _caldavUrl);
                var response = await GetResponseAsync(request);
                
                var taskLists = new List<CalDAVTaskList>();
                
                // Analyser la réponse XML pour extraire les listes de tâches
                var xdoc = XDocument.Parse(response);
                var ns = XNamespace.Get("DAV:");
                
                var responses = xdoc.Descendants(ns + "response");
                foreach (var resp in responses)
                {
                    var href = resp.Descendant(ns + "href")?.Value;
                    var displayName = resp.Descendant(ns + "displayname")?.Value;
                    
                    if (!string.IsNullOrEmpty(href) && href.Contains("/tasks/") || href.Contains("/calendars/"))
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
            catch (Exception ex)
            {
                // En cas d'échec, retourner une liste vide
                return new List<CalDAVTaskList>();
            }
        }
        
        public async Task<List<CalDAVTask>> GetTasksAsync(string taskListId)
        {
            try
            {
                // Construire l'URL de la liste de tâches
                var taskListUrl = $"{_caldavUrl}/{taskListId}";
                
                // Récupérer le contenu iCalendar
                var request = CreateCalDAVRequest("REPORT", taskListUrl);
                request.ContentType = "application/xml";
                
                // Corps de la requête pour récupérer les tâches
                string requestBody = $@"<?xml version='1.0' encoding='utf-8' ?>
<C:calendar-query xmlns:C="urn:ietf:params:xml:ns:caldav">
    <D:prop xmlns:D="DAV:">
        <D:getetag/>
        <C:calendar-data/>
    </D:prop>
    <C:filter>
        <C:comp-filter name="VCALENDAR">
            <C:comp-filter name="VTODO"/>
        </C:comp-filter>
    </C:filter>
</C:calendar-query>";
                
                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(requestBody);
                }
                
                var response = await GetResponseAsync(request);
                
                // Analyser la réponse pour extraire les données iCalendar
                var tasks = new List<CalDAVTask>();
                
                // Cette partie nécessite une analyse plus approfondie de la réponse CalDAV
                // Pour l'instant, nous retournons des données factices
                
                return tasks;
            }
            catch (Exception ex)
            {
                // En cas d'échec, retourner une liste vide
                return new List<CalDAVTask>();
            }
        }
        
        public async Task<CalDAVTask> CreateTaskAsync(string taskListId, CalDAVTask task)
        {
            try
            {
                // Créer un événement iCalendar
                var iCal = new iCalendar();
                var vTodo = iCal.Create<VTodo>();
                
                vTodo.Summary = task.Title;
                vTodo.Description = task.Description;
                vTodo.Due = task.DueDate.HasValue ? new iCalDateTime(task.DueDate.Value) : null;
                vTodo.Status = task.Completed ? "COMPLETED" : "NEEDS-ACTION";
                vTodo.Completed = task.Completed ? new iCalDateTime(task.CompletedDate ?? DateTime.Now) : null;
                vTodo.Created = new iCalDateTime(task.CreatedDate);
                vTodo.LastModified = new iCalDateTime(task.LastModified);
                
                // Sérialiser en format iCalendar
                var serializer = new iCalendarSerializer();
                var calendarData = serializer.SerializeToString(iCal);
                
                // Envoyer au serveur CalDAV
                var taskListUrl = $"{_caldavUrl}/{taskListId}";
                var request = CreateCalDAVRequest("PUT", $"{taskListUrl}/{Guid.NewGuid()}.ics");
                request.ContentType = "text/calendar";
                
                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(calendarData);
                }
                
                var response = await GetResponseAsync(request);
                
                // Mettre à jour l'ID CalDAV de la tâche
                task.CalDAVId = responseHeaders["ETag"];
                
                return task;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create task on CalDAV server: " + ex.Message);
            }
        }
        
        public async Task UpdateTaskAsync(string taskListId, CalDAVTask task)
        {
            try
            {
                // Récupérer la tâche existante
                var existingTask = await GetTaskAsync(taskListId, task.CalDAVId);
                
                // Créer un événement iCalendar mis à jour
                var iCal = new iCalendar();
                var vTodo = iCal.Create<VTodo>();
                
                vTodo.Summary = task.Title;
                vTodo.Description = task.Description;
                vTodo.Due = task.DueDate.HasValue ? new iCalDateTime(task.DueDate.Value) : null;
                vTodo.Status = task.Completed ? "COMPLETED" : "NEEDS-ACTION";
                vTodo.Completed = task.Completed ? new iCalDateTime(task.CompletedDate ?? DateTime.Now) : null;
                vTodo.Created = new iCalDateTime(task.CreatedDate);
                vTodo.LastModified = new iCalDateTime(DateTime.Now);
                vTodo.Uid = new Uri(task.CalDAVId);
                
                // Sérialiser en format iCalendar
                var serializer = new iCalendarSerializer();
                var calendarData = serializer.SerializeToString(iCal);
                
                // Envoyer au serveur CalDAV
                var taskUrl = $"{_caldavUrl}/{taskListId}/{task.CalDAVId}.ics";
                var request = CreateCalDAVRequest("PUT", taskUrl);
                request.ContentType = "text/calendar";
                request.Headers.Add("If-Match", task.CalDAVEtag);
                
                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(calendarData);
                }
                
                var response = await GetResponseAsync(request);
                
                // Mettre à jour l'ETag
                task.CalDAVEtag = responseHeaders["ETag"];
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update task on CalDAV server: " + ex.Message);
            }
        }
        
        public async Task DeleteTaskAsync(string taskListId, string taskId)
        {
            try
            {
                var taskUrl = $"{_caldavUrl}/{taskListId}/{taskId}.ics";
                var request = CreateCalDAVRequest("DELETE", taskUrl);
                
                await GetResponseAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete task from CalDAV server: " + ex.Message);
            }
        }
        
        public async Task SyncTasksAsync(string taskListId, List<CalDAVTask> localTasks)
        {
            try
            {
                // 1. Récupérer les tâches distantes
                var remoteTasks = await GetTasksAsync(taskListId);
                
                // 2. Synchroniser les tâches
                // Cette implémentation est simplifiée - une implémentation complète
                // devrait gérer les conflits, les suppressions, etc.
                
                foreach (var localTask in localTasks)
                {
                    var remoteTask = remoteTasks.FirstOrDefault(t => t.CalDAVId == localTask.CalDAVId);
                    
                    if (remoteTask == null)
                    {
                        // Tâche locale nouvelle - créer sur le serveur
                        await CreateTaskAsync(taskListId, localTask);
                    }
                    else if (remoteTask.LastModified > localTask.LastModified)
                    {
                        // Tâche distante plus récente - mettre à jour localement
                        // (implémentation simplifiée)
                    }
                    else if (localTask.LastModified > remoteTask.LastModified)
                    {
                        // Tâche locale plus récente - mettre à jour sur le serveur
                        await UpdateTaskAsync(taskListId, localTask);
                    }
                }
                
                // 3. Supprimer les tâches qui existent sur le serveur mais pas localement
                foreach (var remoteTask in remoteTasks)
                {
                    if (!localTasks.Any(t => t.CalDAVId == remoteTask.CalDAVId))
                    {
                        await DeleteTaskAsync(taskListId, remoteTask.CalDAVId);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to sync tasks with CalDAV server: " + ex.Message);
            }
        }
        
        private HttpWebRequest CreateCalDAVRequest(string method, string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Credentials = new NetworkCredential(_username, _password);
            request.PreAuthenticate = true;
            request.Headers.Add("Depth", "1");
            request.Accept = "application/xml,text/xml,text/calendar";
            request.UserAgent = "Palisades/1.0";
            return request;
        }
        
        private async Task<string> GetResponseAsync(HttpWebRequest request)
        {
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
        
        private async Task<CalDAVTask> GetTaskAsync(string taskListId, string taskId)
        {
            // Implémentation pour récupérer une tâche spécifique
            // (à compléter)
            return new CalDAVTask();
        }
    }
}