using Palisades.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace Palisades.View
{
    public partial class MailPalisade : Window
    {
        public MailPalisade(MailPalisadeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Show();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}
