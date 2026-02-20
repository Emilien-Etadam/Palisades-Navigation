using Palisades.Model;
using System;
using System.Collections.Generic;
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

namespace Palisades.Services
{
    /// <summary>
    /// Service CalDAV pour les calendriers (VEVENT). Réutilise le même schéma d'authentification que CalDAVService.
    /// </summary>
    public class CalendarCalDAVService
    {
        private readonly string _caldavBaseUrl;
        private readonly HttpClient _httpClient;

        public CalendarCalDAVService(string caldavBaseUrl, string username, string password)
        {
            _caldavBaseUrl = (caldavBaseUrl ?? "").TrimEnd('/');
            if (!string.IsNullOrEmpty(_caldavBaseUrl) && !_caldavBaseUrl.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("L'URL CalDAV doit utiliser HTTPS.");

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
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/calendar"));
        }

        /// <summary>
        /// Découvre les collections de type calendrier (PROPFIND).
        /// Pour Zimbra : souvent sous /Calendar ou /Calendars.
        /// </summary>
        public async Task<List<CalDAVCalendarInfo>> GetCalendarListAsync()
        {
            var list = new List<CalDAVCalendarInfo>();
            try
            {
                string body = @"<?xml version='1.0' encoding='utf-8' ?>
<D:propfind xmlns:D=""DAV:"">
  <D:prop>
    <D:resourcetype/>
    <D:displayname/>
  </D:prop>
</D:propfind>";
                var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), _caldavBaseUrl);
                request.Content = new StringContent(body, Encoding.UTF8, "application/xml");
                request.Headers.Add("Depth", "1");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var xml = await response.Content.ReadAsStringAsync();
                var dav = XNamespace.Get("DAV:");
                var xdoc = XDocument.Parse(xml);
                foreach (var resp in xdoc.Descendants(dav + "response"))
                {
                    var href = resp.Descendants(dav + "href").FirstOrDefault()?.Value?.TrimEnd('/');
                    if (string.IsNullOrEmpty(href)) continue;
                    var propstat = resp.Descendants(dav + "propstat").FirstOrDefault(p => p.Element(dav + "status")?.Value?.Contains("200") == true);
                    var prop = propstat?.Element(dav + "prop");
                    if (prop == null) continue;
                    var resourcetype = prop.Element(dav + "resourcetype");
                    var isCalendar = resourcetype?.Descendants().Any(e => e.Name.LocalName.Equals("calendar", StringComparison.OrdinalIgnoreCase)) == true;
                    if (!isCalendar) continue;
                    var displayName = prop.Element(dav + "displayname")?.Value ?? (href.Contains('/') ? href.Substring(href.LastIndexOf('/') + 1) : href);
                    var calendarId = href.Contains('/') ? href.Substring(href.LastIndexOf('/') + 1) : href;
                    list.Add(new CalDAVCalendarInfo
                    {
                        CalendarId = calendarId,
                        DisplayName = displayName,
                        Href = href
                    });
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// Récupère les événements (VEVENT) dans la plage [start, end] pour un calendrier.
        /// calendarIdOrHref : id court (nom de collection) ou URL complète.
        /// </summary>
        public async Task<List<CalendarEvent>> GetEventsAsync(string calendarIdOrHref, DateTime start, DateTime end)
        {
            var events = new List<CalendarEvent>();
            string calendarUrl = calendarIdOrHref.Contains("http") ? calendarIdOrHref : $"{_caldavBaseUrl}/{calendarIdOrHref}";
            try
            {
                string startUtc = start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                string endUtc = end.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                string requestBody = $@"<?xml version='1.0' encoding='utf-8' ?>
<C:calendar-query xmlns:C=""urn:ietf:params:xml:ns:caldav"" xmlns:D=""DAV:"">
  <D:prop>
    <D:getetag/>
    <C:calendar-data/>
  </D:prop>
  <C:filter>
    <C:comp-filter name=""VCALENDAR"">
      <C:comp-filter name=""VEVENT"">
        <C:time-range start=""{startUtc}"" end=""{endUtc}""/>
      </C:comp-filter>
    </C:comp-filter>
  </C:filter>
</C:calendar-query>";
                var request = new HttpRequestMessage(new HttpMethod("REPORT"), calendarUrl);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/xml");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                ParseEventsFromMultistatus(body, calendarIdOrHref, "Calendar", System.Windows.Media.Colors.SlateGray, events);
            }
            catch { }
            return events;
        }

        /// <summary>
        /// Parse une réponse multistatus CalDAV et remplit la liste d'événements.
        /// </summary>
        private static void ParseEventsFromMultistatus(string multistatusXml, string calendarId, string calendarName, System.Windows.Media.Color defaultColor, List<CalendarEvent> events)
        {
            var dav = XNamespace.Get("DAV:");
            var caldav = XNamespace.Get("urn:ietf:params:xml:ns:caldav");
            try
            {
                var xdoc = XDocument.Parse(multistatusXml);
                foreach (var response in xdoc.Descendants(dav + "response"))
                {
                    var href = response.Descendants(dav + "href").FirstOrDefault()?.Value?.TrimEnd('/');
                    if (string.IsNullOrEmpty(href)) continue;
                    var propstat = response.Descendants(dav + "propstat").FirstOrDefault(p => p.Element(dav + "status")?.Value?.Contains("200") == true);
                    var prop = propstat?.Element(dav + "prop");
                    if (prop == null) continue;
                    var etag = prop.Element(dav + "getetag")?.Value?.Trim('"') ?? "";
                    var calendarDataEl = prop.Element(caldav + "calendar-data");
                    var calendarData = calendarDataEl?.Value;
                    if (string.IsNullOrWhiteSpace(calendarData)) continue;
                    try
                    {
                        var calendar = Ical.Net.Calendar.Load(calendarData);
                        foreach (var evt in calendar.Events)
                        {
                            events.Add(MapIcalEventToCalendarEvent(evt, href, etag, calendarName, defaultColor));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static CalendarEvent MapIcalEventToCalendarEvent(Ical.Net.CalendarComponents.CalendarEvent evt, string caldavHref, string etag, string calendarName, System.Windows.Media.Color color)
        {
            var start = evt.Start?.Value ?? DateTime.MinValue;
            var end = evt.End?.Value ?? evt.Start?.Value ?? DateTime.MinValue;
            return new CalendarEvent
            {
                Uid = evt.Uid ?? "",
                Summary = evt.Summary ?? "",
                Description = evt.Description ?? "",
                DtStart = start,
                DtEnd = end,
                Location = evt.Location ?? "",
                IsAllDay = evt.IsAllDay,
                CalendarName = calendarName,
                Color = color,
                CalDAVHref = caldavHref,
                ETag = etag
            };
        }
    }

    public class CalDAVCalendarInfo
    {
        public string CalendarId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Href { get; set; } = "";
    }
}
