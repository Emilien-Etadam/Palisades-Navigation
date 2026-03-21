using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Palisades.Helpers;
using Palisades.Services;
using Palisades.View;
using System.Windows.Threading;

namespace Palisades
{
    public partial class App : System.Windows.Application
    {
        private TrayIconManager? _trayIcon;

        [Conditional("DEBUG")]
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

        private static void WriteCrashLog(string context, Exception? ex)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "Palisades_startup.log");
                var line = DateTime.Now.ToString("o") + " " + context + Environment.NewLine;
                if (ex != null)
                    line += ex + Environment.NewLine;
                File.AppendAllText(path, line);
            }
            catch { /* ignore */ }
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                WriteCrashLog("UnhandledException", e.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            WriteStartupLog("App() début");

            try
            {
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

                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(100);
                    foreach (var kv in PalisadesManager.palisades)
                    {
                        if (kv.Value is Window w)
                        { w.Activate(); break; }
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                WriteCrashLog("EXCEPTION au démarrage", ex);
                try
                {
                    MessageBox.Show(
                        "Erreur au démarrage : " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace,
                        "Palisades - Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { WriteCrashLog("MessageBox a échoué", null); }
                Shutdown();
                return;
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WriteCrashLog("DispatcherUnhandledException", e.Exception);
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
                var progressHandler = new Progress<double>(percent =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (Application.Current.MainWindow != null)
                                Application.Current.MainWindow.Title = $"Palisades — Downloading update: {percent:F0}%";
                        }
                        catch { }
                    });
                });
                await UpdateChecker.ApplyUpdateAsync(update, progressHandler);
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
