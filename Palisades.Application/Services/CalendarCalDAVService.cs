using Palisades.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using System.Windows.Media;

namespace Palisades.Services
{
    /// <summary>
    /// Service CalDAV pour les calendriers (VEVENT). Utilise CalDAVClient pour le transport.
    /// </summary>
    public class CalendarCalDAVService
    {
        private readonly CalDAVClient _client;

        public CalendarCalDAVService(CalDAVClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Découvre les collections de type calendrier (délègue à CalDAVClient.DiscoverCalendarsAsync).
        /// </summary>
        public async Task<List<CalDAVCalendarInfo>> GetCalendarListAsync()
        {
            return await _client.DiscoverCalendarsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Récupère les événements (VEVENT) dans la plage [start, end] pour un calendrier.
        /// calendarHref : HREF complet de la collection calendrier.
        /// </summary>
        public async Task<List<Model.CalendarEvent>> GetEventsAsync(string calendarHref, DateTime start, DateTime end)
        {
            var events = new List<Model.CalendarEvent>();
            var startUtc = start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var endUtc = end.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var requestBody = $@"<?xml version='1.0' encoding='utf-8' ?>
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

            var doc = await _client.ReportAsync(calendarHref, requestBody).ConfigureAwait(false);
            ParseEventsFromMultistatus(doc, calendarHref, "Calendar", Colors.SlateGray, events);
            return events;
        }

        private static void ParseEventsFromMultistatus(XDocument xdoc, string calendarId, string calendarName, Color defaultColor, List<Model.CalendarEvent> events)
        {
            var responses = CalDAVClient.ParseMultistatus(xdoc);
            foreach (var (href, props, _) in responses)
            {
                if (string.IsNullOrEmpty(href))
                    continue;
                if (!props.TryGetValue("getetag", out var etag))
                    etag = "";
                etag = etag.Trim('"');
                if (!props.TryGetValue("calendar-data", out var calendarData) || string.IsNullOrWhiteSpace(calendarData))
                    continue;
                try
                {
                    var calendar = Ical.Net.Calendar.Load(calendarData);
                    foreach (var evt in calendar.Events)
                        events.Add(MapIcalEventToCalendarEvent(evt, href, etag, calendarName, defaultColor));
                }
                catch
                {
                    /* ignorer un bloc calendar invalide */
                }
            }
        }

        private static Model.CalendarEvent MapIcalEventToCalendarEvent(Ical.Net.CalendarComponents.CalendarEvent evt, string caldavHref, string etag, string calendarName, Color color)
        {
            var start = evt.Start?.Value ?? DateTime.MinValue;
            var end = evt.End?.Value ?? evt.Start?.Value ?? DateTime.MinValue;
            return new Model.CalendarEvent
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
