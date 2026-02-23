using System;

namespace Palisades.Model
{
    public class CalendarEvent
    {
        public string Uid { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DtStart { get; set; }
        public DateTime DtEnd { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool IsAllDay { get; set; }
        public string CalendarName { get; set; } = string.Empty;
        public string Color { get; set; } = "#708090";
        public string CalDAVHref { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
    }
}
