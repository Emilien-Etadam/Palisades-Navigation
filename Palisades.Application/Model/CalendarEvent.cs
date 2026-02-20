using System;
using System.Windows.Media;

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
        public Color Color { get; set; }
        public string CalDAVHref { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
    }
}
