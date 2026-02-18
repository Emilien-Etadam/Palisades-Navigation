
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Xml.Serialization;
namespace Palisades.Model
{
    [XmlRoot(Namespace = "io.stouder", ElementName = "PalisadeModel")]
    public class PalisadeModel
    {
        private string identifier;
        private string name;
        private int fenceX;
        private int fenceY;
        private int width;
        private int height;
        private ObservableCollection<Shortcut> shortcuts;
        private Color headerColor;
        private Color bodyColor;
        private Color titleColor;
        private Color labelsColor;
        private PalisadeType type;
        private string rootPath;
        private string currentPath;

        public PalisadeModel()
        {
            identifier = Guid.NewGuid().ToString();
            name = "No name";
            headerColor = Color.FromArgb(200, 0, 0, 0);
            bodyColor = Color.FromArgb(120, 0, 0, 0);
            titleColor = Color.FromArgb(255, 255, 255, 255);
            labelsColor = Color.FromArgb(255, 255, 255, 255);
            width = 800;
            height = 450;
            shortcuts = new();
            type = PalisadeType.Standard;
            rootPath = "";
            currentPath = "";
        }

        public string Identifier { get { return identifier; } set { identifier = value; } }
        public string Name { get { return name; } set { name = value; } }

        public int FenceX { get { return fenceX; } set { fenceX = value; } }
        public int FenceY { get { return fenceY; } set { fenceY = value; } }

        public int Width { get { return width; } set { width = value; } }
        public int Height { get { return height; } set { height = value; } }

        public Color HeaderColor { get { return headerColor; } set { headerColor = value; } }
        public Color BodyColor { get { return bodyColor; } set { bodyColor = value; } }
        public Color TitleColor { get { return titleColor; } set { titleColor = value; } }
        public Color LabelsColor { get { return labelsColor; } set { labelsColor = value; } }

        public PalisadeType Type { get { return type; } set { type = value; } }
        public string RootPath { get { return rootPath; } set { rootPath = value; } }
        public string CurrentPath { get { return currentPath; } set { currentPath = value; } }

        // Task Palisade properties
        public string CalDAVUrl { get; set; } = string.Empty;
        public string CalDAVUsername { get; set; } = string.Empty;
        public string CalDAVPassword { get; set; } = string.Empty;
        public string TaskListId { get; set; } = string.Empty;
        public int SyncIntervalMinutes { get; set; } = 5;
        public bool EnableLogging { get; set; } = false;
        public bool ShowCompletedTasks { get; set; } = true;

        [XmlArrayItem(typeof(LnkShortcut))]
        [XmlArrayItem(typeof(UrlShortcut))]
        public ObservableCollection<Shortcut> Shortcuts { get { return shortcuts; } set { shortcuts = value; } }
    }
}
