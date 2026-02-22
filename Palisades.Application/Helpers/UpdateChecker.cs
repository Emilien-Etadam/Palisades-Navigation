using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Palisades.Helpers
{
    public static class UpdateChecker
    {
        private const string ReleasesUrl =
            "https://api.github.com/repos/Emilien-Etadam/Palisades-Navigation/releases/latest";

        public static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly()
                       .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                       ?.InformationalVersion?.Split('+')[0]
                   ?? "0.0.0";
        }

        public static async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                using var client = CreateClient();
                var json = await client.GetStringAsync(ReleasesUrl).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString() ?? "";
                var remoteVersion = tagName.TrimStart('v', 'V');

                if (!Version.TryParse(remoteVersion, out var remote) ||
                    !Version.TryParse(GetCurrentVersion(), out var current) ||
                    remote <= current)
                    return null;

                string? assetUrl = null;
                foreach (var asset in root.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        assetUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(assetUrl))
                    return null;

                var notes = root.GetProperty("body").GetString() ?? "";
                return new UpdateInfo(remoteVersion, assetUrl, notes);
            }
            catch
            {
                return null;
            }
        }

        public static async Task ApplyUpdateAsync(UpdateInfo update)
        {
            var tempInstaller = Path.Combine(Path.GetTempPath(), $"Palisades-{update.Version}-setup.exe");

            using (var client = CreateClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                var bytes = await client.GetByteArrayAsync(update.AssetUrl).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempInstaller, bytes).ConfigureAwait(false);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = tempInstaller,
                Arguments = "/SILENT /RESTARTAPPLICATIONS",
                UseShellExecute = true
            });
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Palisades", GetCurrentVersion()));
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }
    }

    public record UpdateInfo(string Version, string AssetUrl, string ReleaseNotes);
}
