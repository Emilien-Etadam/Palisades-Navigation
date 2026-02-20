using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Palisades.Model
{
    /// <summary>
    /// Modèle monolithique conservé pour la rétrocompatibilité : les anciens state.xml
    /// sans xsi:type se désérialisent dans ce type. Converti ensuite en StandardPalisadeModel /
    /// FolderPortalModel / TaskPalisadeModel selon Type.
    /// </summary>
    [XmlRoot(Namespace = "io.stouder", ElementName = "PalisadeModel")]
    public class PalisadeModel : PalisadeModelBase
    {
        private string _rootPath = "";
        private string _currentPath = "";
        private ObservableCollection<Shortcut> _shortcuts = new();

        public PalisadeModel()
        {
            _shortcuts = new ObservableCollection<Shortcut>();
        }

        public string RootPath { get => _rootPath; set => _rootPath = value ?? ""; }
        public string CurrentPath { get => _currentPath; set => _currentPath = value ?? ""; }

        public string CalDAVUrl { get; set; } = string.Empty;
        public string CalDAVUsername { get; set; } = string.Empty;
        public string CalDAVPassword { get; set; } = string.Empty;
        public string TaskListId { get; set; } = string.Empty;
        public int SyncIntervalMinutes { get; set; } = 5;
        public bool EnableLogging { get; set; }
        public bool ShowCompletedTasks { get; set; } = true;

        [XmlArrayItem(typeof(LnkShortcut))]
        [XmlArrayItem(typeof(UrlShortcut))]
        public ObservableCollection<Shortcut> Shortcuts { get => _shortcuts; set => _shortcuts = value; }
    }
}
