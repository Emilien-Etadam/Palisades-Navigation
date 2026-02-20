using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Palisades.Helpers;
using Sentry;
using System.Windows.Threading;

namespace Palisades
{
    public partial class App : System.Windows.Application
    {
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

                PalisadesManager.LoadPalisades();
                WriteStartupLog("LoadPalisades() ok, count=" + PalisadesManager.palisades.Count);

                if (PalisadesManager.palisades.Count == 0)
                {
                    PalisadesManager.CreatePalisade();
                    WriteStartupLog("CreatePalisade() ok");
                }
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
    }
}
