using System;
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
        private PalisadeType _type;

        protected PalisadeModelBase()
        {
            _identifier = Guid.NewGuid().ToString();
            _name = "No name";
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
        public PalisadeType Type { get => _type; set => _type = value; }
        /// <summary>Groupe d'onglets (Phase 10.2). Null = palisade autonome.</summary>
        public string? GroupId { get; set; }
        /// <summary>Position de l'onglet dans le groupe (défaut 0).</summary>
        public int TabOrder { get; set; }
    }
}
