using System;
using System.Xml.Serialization;
using Palisades.Model;

namespace Palisades.Serialization
{
    /// <summary>
    /// Point unique pour le <see cref="XmlSerializer"/> des états palisade (<c>state.xml</c>, snapshots).
    /// Tout nouveau type sérialisé sous <see cref="PalisadeModelBase"/> doit être ajouté ici et dans les attributs <c>[XmlInclude]</c> du modèle.
    /// </summary>
    public static class PalisadeXmlSerialization
    {
        /// <summary>Types additionnels pour le polymorphisme XML (héritiers de <see cref="PalisadeModelBase"/> et raccourcis).</summary>
        public static Type[] ExtraModelTypes { get; } =
        {
            typeof(PalisadeModel),
            typeof(StandardPalisadeModel),
            typeof(FolderPortalModel),
            typeof(TaskPalisadeModel),
            typeof(CalendarPalisadeModel),
            typeof(MailPalisadeModel),
            typeof(Shortcut),
            typeof(LnkShortcut),
            typeof(UrlShortcut),
        };

        /// <summary>Sérialiseur partagé : même instance pour ViewModelBase, LayoutSnapshotService et chargement initial.</summary>
        public static readonly XmlSerializer PalisadeModelSerializer = new(typeof(PalisadeModelBase), ExtraModelTypes);
    }
}
