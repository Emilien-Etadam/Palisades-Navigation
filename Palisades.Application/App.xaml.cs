using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Palisades.Helpers;
using Palisades.Services;
using Palisades.View;
using Sentry;
using System.Windows.Threading;

namespace Palisades
{
    public partial class App : System.Windows.Application
    {
        private TrayIconManager? _trayIcon;

        private static void WriteStartupLog(string message, Exception? ex = null)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "Palisades_startup.log");
                string line = DateTime.Now.ToString("o") + " " + message;
                if (ex != null)
                    line += Environment.NewLine + ex.ToString();
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* ignore */ }
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                WriteStartupLog("UnhandledException", e.ExceptionObject as Exception);
            };

            WriteStartupLog("App() début");

            try
            {
                SetupSentry();
                WriteStartupLog("SetupSentry() ok");

                ThemeWatcher.Apply(Resources);

                PalisadesManager.LoadPalisades();
                WriteStartupLog("LoadPalisades() ok, count=" + PalisadesManager.palisades.Count);

                if (PalisadesManager.palisades.Count == 0)
                {
                    PalisadesManager.CreatePalisade();
                    WriteStartupLog("CreatePalisade() ok");
                }

                var overlay = new DesktopDrawingOverlay();
                overlay.Show();
                overlay.ShowActivated = false;
                foreach (var kv in PalisadesManager.palisades)
                {
                    if (kv.Value is Window w)
                    { w.Activate(); break; }
                }
                WriteStartupLog("DesktopDrawingOverlay shown");

                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                _trayIcon = new TrayIconManager();
                _ = RunAutoUpdateAsync();
                WriteStartupLog("TrayIcon created");

                Exit += (_, _) =>
                {
                    Helpers.ToastHelper.Cleanup();
                    _trayIcon?.Dispose();
                    LayoutSnapshotService.SaveAutoSnapshotAndPrune(3);
                };
            }
            catch (Exception ex)
            {
                WriteStartupLog("EXCEPTION", ex);
                try
                {
                    MessageBox.Show(
                        "Erreur au démarrage : " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace,
                        "Palisades - Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { WriteStartupLog("MessageBox a échoué"); }
                Shutdown();
                return;
            }
        }

        private static string GetSentryDsn()
        {
            var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
            if (!string.IsNullOrWhiteSpace(dsn))
                return dsn.Trim();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            var config = builder.Build();
            return config["Sentry:Dsn"] ?? string.Empty;
        }

        private void SetupSentry()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            var dsn = GetSentryDsn();
            if (string.IsNullOrWhiteSpace(dsn))
                return;

            SentrySdk.Init(o =>
            {
                o.Dsn = dsn;
                o.Debug = PEnv.IsDev();
                o.TracesSampleRate = 1;
            });
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);
            e.Handled = true;
        }

        private static async Task RunAutoUpdateAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(8));
            var update = await UpdateChecker.CheckAsync();
            if (update == null) return;

            var result = Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Version {update.Version} disponible.\n\n{update.ReleaseNotes}\n\nInstaller et relancer ?",
                    "Mise à jour Palisades",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information));

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await UpdateChecker.ApplyUpdateAsync(update);
                Current.Dispatcher.Invoke(() => Current.Shutdown());
            }
            catch (Exception ex)
            {
                Current.Dispatcher.Invoke(() =>
                    MessageBox.Show($"Échec de la mise à jour : {ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }
    }
}
