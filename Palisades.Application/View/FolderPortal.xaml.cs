using Palisades.ViewModel;
using System.Windows;

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

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}
