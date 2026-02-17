using Palisades.Helpers;
using Palisades.Model;
using Palisades.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Palisades.View
{
    public partial class FolderPortal : Window
    {
        private readonly FolderPortalViewModel viewModel;

        public FolderPortal(FolderPortalViewModel defaultModel)
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

        private void Item_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FolderPortalItem item)
            {
                e.Handled = true;
                ShellContextMenu.Show(item.FullPath, this);
                // Refresh after context menu closes (file may have been renamed, deleted, etc.)
                viewModel.RefreshCommand.Execute(null);
            }
        }
    }
}
