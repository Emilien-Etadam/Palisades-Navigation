using System;
using System.Xml.Serialization;

namespace Palisades.Model
{
    [Serializable]
    public class CalDAVTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        /// <summary>Chemin/href serveur (ex. nom fichier .ics).</summary>
        public string CalDAVId { get; set; } = string.Empty;
        /// <summary>Uid iCalendar (identifiant interne du VTODO).</summary>
        public string Uid { get; set; } = string.Empty;
        public string CalDAVEtag { get; set; } = string.Empty;
        
        public CalDAVTask() { }
        
        public CalDAVTask(string title)
        {
            Title = title;
        }
    }
}