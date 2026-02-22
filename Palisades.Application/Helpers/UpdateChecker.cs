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

        public static async Task ApplyUpdateAsync(UpdateInfo update, IProgress<double>? progress = null)
        {
            var tempInstaller = Path.Combine(Path.GetTempPath(), $"Palisades-{update.Version}-setup.exe");

            using (var client = CreateClient())
            {
                using var response = await client.GetAsync(update.AssetUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var fileStream = new FileStream(tempInstaller, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                    totalRead += bytesRead;
                    if (totalBytes > 0)
                        progress?.Report((double)totalRead / totalBytes * 100);
                }
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
