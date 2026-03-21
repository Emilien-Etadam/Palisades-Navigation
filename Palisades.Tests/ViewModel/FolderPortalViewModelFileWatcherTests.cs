using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Palisades.Model;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests.ViewModel
{
    /// <summary>
    /// Nécessite un thread STA et une <see cref="Dispatcher"/> WPF : le watcher déclenche un timer
    /// qui rappelle l’UI via <c>Dispatcher.Invoke</c> ; sans Application, le test est exécuté sur un
    /// thread STA dédié avec pompage du dispatcher.
    /// </summary>
    public class FolderPortalViewModelFileWatcherTests
    {
        [Fact]
        public void FileSystemWatcher_NewFileAppearsInItems_AfterDebounce()
        {
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                    var tempDir = Path.Combine(Path.GetTempPath(), "PalisadesWatcherTest_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);
                    try
                    {
                        var model = new FolderPortalModel
                        {
                            Name = "Test",
                            RootPath = tempDir,
                            CurrentPath = tempDir,
                        };
                        var vm = new FolderPortalViewModel(model);

                        Assert.Empty(vm.Items);

                        var testFile = Path.Combine(tempDir, "test.txt");
                        File.WriteAllText(testFile, "content");

                        var deadline = DateTime.UtcNow.AddMilliseconds(1500);
                        while (DateTime.UtcNow < deadline)
                        {
                            Application.Current!.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(10);
                        }

                        Assert.Contains(vm.Items, i => i.Name == "test.txt");
                    }
                    finally
                    {
                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    Application.Current?.Shutdown();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(20000);
            if (error != null)
            {
                throw new AggregateException(error);
            }
        }
    }
}
