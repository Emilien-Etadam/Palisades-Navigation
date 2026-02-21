using System;
using System.IO;

namespace Palisades.Helpers
{
    internal static class PDirectory
    {
        internal static string GetAppDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PEnv.IsDev() ? "PalisadesDev" : "Palisades");
        }

        internal static string GetPalisadesDirectory()
        {
            return Path.Combine(GetAppDirectory(), "saved");
        }

        internal static string GetPalisadeDirectory(string identifier)
        {
            return Path.Combine(GetPalisadesDirectory(), identifier);
        }

        internal static string GetPalisadeIconsDirectory(string identifier)
        {
            return Path.Combine(GetPalisadeDirectory(identifier), "icons");
        }

        internal static string GetAccountsFilePath()
        {
            return Path.Combine(GetAppDirectory(), "accounts.xml");
        }

        internal static string GetSnapshotsDirectory()
        {
            return Path.Combine(GetAppDirectory(), "snapshots");
        }

        internal static string GetSettingsFilePath()
        {
            return Path.Combine(GetAppDirectory(), "settings.xml");
        }

        internal static void EnsureExists(string directory)
        {
            DirectoryInfo infos = new(directory);
            if (!infos.Exists)
            {
                infos.Create();
            }
        }
    }
}
