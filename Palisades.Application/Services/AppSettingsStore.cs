using Palisades.Helpers;
using Palisades.Model;
using System.IO;
using System.Xml.Serialization;

namespace Palisades.Services
{
    /// <summary>
    /// Persistance des paramètres globaux (Phase 10.2). Fichier %LOCALAPPDATA%\Palisades\settings.xml.
    /// </summary>
    public static class AppSettingsStore
    {
        private static readonly XmlSerializer Serializer = new(typeof(AppSettings));

        public static AppSettings Load()
        {
            var path = PDirectory.GetSettingsFilePath();
            if (!File.Exists(path))
                return new AppSettings();
            try
            {
                using var reader = new StreamReader(path);
                if (Serializer.Deserialize(reader) is AppSettings settings)
                    return settings;
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            var path = PDirectory.GetSettingsFilePath();
            PDirectory.EnsureExists(Path.GetDirectoryName(path)!);
            using var writer = new StreamWriter(path);
            Serializer.Serialize(writer, settings ?? new AppSettings());
        }
    }