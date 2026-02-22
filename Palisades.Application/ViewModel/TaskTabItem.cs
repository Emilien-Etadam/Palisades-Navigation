using System.Collections.ObjectModel;
using Palisades.Model;

namespace Palisades.ViewModel
{
    public class TaskTabItem
    {
        public string ListId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ObservableCollection<CalDAVTask> Tasks { get; } = new();
    }
}
