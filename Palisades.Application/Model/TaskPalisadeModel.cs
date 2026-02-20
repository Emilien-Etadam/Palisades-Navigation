using System;

namespace Palisades.Model
{
    public class TaskPalisadeModel : PalisadeModelBase
    {
        public TaskPalisadeModel()
        {
            Type = PalisadeType.TaskPalisade;
        }

        /// <summary>Si défini, les credentials sont résolus depuis ZimbraAccountStore (Phase 3.4).</summary>
        public Guid? ZimbraAccountId { get; set; }

        public string CalDAVUrl { get; set; } = string.Empty;
        public string CalDAVUsername { get; set; } = string.Empty;
        public string CalDAVPassword { get; set; } = string.Empty;
        public string TaskListId { get; set; } = string.Empty;
        public int SyncIntervalMinutes { get; set; } = 5;
        public bool EnableLogging { get; set; }
        public bool ShowCompletedTasks { get; set; } = true;
    }
}
