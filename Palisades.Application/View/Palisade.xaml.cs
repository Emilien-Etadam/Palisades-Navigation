using Palisades.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Palisades.View
{
    public partial class Palisade : Window
    {
        private readonly PalisadeViewModel viewModel;
        public Palisade(PalisadeViewModel defaultModel)
        {
            InitializeComponent();
            DataContext = defaultModel;
            viewModel = defaultModel;
            Show();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void LayoutsSubmenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
                viewModel.RefreshRecentSnapshots();
        }

        private void OnExternalFileDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void OnExternalFileDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || DataContext is not ViewModel.PalisadeViewModel vm) return;
                foreach (var filePath in files)
                {
                    if (string.IsNullOrEmpty(filePath)) continue;
                    var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    var ext = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
                    Model.Shortcut sc = ext == ".url"
                        ? new Model.UrlShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath }
                        : new Model.LnkShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath };
                    vm.Shortcuts.Add(sc);
                }
                vm.Save();
                e.Handled = true;
            }
        }
    }
}
