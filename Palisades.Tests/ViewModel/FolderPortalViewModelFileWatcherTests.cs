using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Palisades.Model;
using Palisades.ViewModel;
using Xunit;

namespace Palisades.Tests.ViewModel
{
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

                        var testFile = Path.Combine(tempDir, "watcher_new_file.txt");
                        File.WriteAllText(testFile, "x");

                        var deadline = DateTime.UtcNow.AddSeconds(5);
                        while (DateTime.UtcNow < deadline &&
                               !vm.Items.Any(i => i.Name == "watcher_new_file.txt"))
                        {
                            Application.Current!.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(50);
                        }

                        Assert.Contains(vm.Items, i => i.Name == "watcher_new_file.txt");
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
