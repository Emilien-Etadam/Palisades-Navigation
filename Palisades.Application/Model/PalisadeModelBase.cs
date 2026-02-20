using System;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Palisades.Model
{
    /// <summary>
    /// Modèle de base commun à toutes les palisades. Les sous-types sont utilisés pour la sérialisation polymorphe.
    /// </summary>
    [XmlInclude(typeof(PalisadeModel))]
    [XmlInclude(typeof(StandardPalisadeModel))]
    [XmlInclude(typeof(FolderPortalModel))]
    [XmlInclude(typeof(TaskPalisadeModel))]
    [XmlInclude(typeof(CalendarPalisadeModel))]
    [XmlInclude(typeof(MailPalisadeModel))]
    [XmlRoot(Namespace = "io.stouder", ElementName = "PalisadeModel")]
    public abstract class PalisadeModelBase
    {
        private string _identifier;
        private string _name;
        private int _fenceX;
        private int _fenceY;
        private int _width;
        private int _height;
        private Color _headerColor;
        private Color _bodyColor;
        private Color _titleColor;
        private Color _labelsColor;
        private PalisadeType _type;

        protected PalisadeModelBase()
        {
            _identifier = Guid.NewGuid().ToString();
            _name = "No name";
            _headerColor = Color.FromArgb(200, 0, 0, 0);
            _bodyColor = Color.FromArgb(120, 0, 0, 0);
            _titleColor = Color.FromArgb(255, 255, 255, 255);
            _labelsColor = Color.FromArgb(255, 255, 255, 255);
            _width = 800;
            _height = 450;
            _type = PalisadeType.Standard;
        }

        public string Identifier { get => _identifier; set => _identifier = value; }
        public string Name { get => _name; set => _name = value; }
        public int FenceX { get => _fenceX; set => _fenceX = value; }
        public int FenceY { get => _fenceY; set => _fenceY = value; }
        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        public Color HeaderColor { get => _headerColor; set => _headerColor = value; }
        public Color BodyColor { get => _bodyColor; set => _bodyColor = value; }
        public Color TitleColor { get => _titleColor; set => _titleColor = value; }
        public Color LabelsColor { get => _labelsColor; set => _labelsColor = value; }
        public PalisadeType Type { get => _type; set => _type = value; }
    }
}
