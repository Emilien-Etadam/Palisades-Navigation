using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Palisades.Model
{
    [XmlType(Namespace = "io.stouder")]
    public class StandardPalisadeModel : PalisadeModelBase
    {
        private ObservableCollection<Shortcut> _shortcuts = new();

        public StandardPalisadeModel()
        {
            Type = PalisadeType.Standard;
        }

        [XmlArrayItem(typeof(LnkShortcut))]
        [XmlArrayItem(typeof(UrlShortcut))]
        public ObservableCollection<Shortcut> Shortcuts { get => _shortcuts; set => _shortcuts = value; }
    }
}
