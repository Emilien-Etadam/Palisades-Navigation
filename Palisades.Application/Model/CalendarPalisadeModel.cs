using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Palisades.Model
{
    [XmlType(Namespace = "io.stouder")]
    public class CalendarPalisadeModel : PalisadeModelBase
    {
        public CalendarPalisadeModel()
        {
            Type = PalisadeType.CalendarPalisade;
        }

        /// <summary>Si défini, les credentials viennent de ZimbraAccountStore.</summary>
        public Guid? ZimbraAccountId { get; set; }
        public string CalDAVBaseUrl { get; set; } = string.Empty;
        public string CalDAVUsername { get; set; } = string.Empty;
        public string CalDAVPassword { get; set; } = string.Empty;
        public List<string> CalendarIds { get; set; } = new List<string>();
        public CalendarViewMode ViewMode { get; set; } = CalendarViewMode.Agenda;
        public int DaysToShow { get; set; } = 7;
    }

    public enum CalendarViewMode
    {
        Agenda,
        Day,
        Week
    }
}
