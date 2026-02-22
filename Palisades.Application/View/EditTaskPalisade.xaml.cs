using Palisades.ViewModel;
using System.Windows;

namespace Palisades.View
{
    public partial class EditTaskPalisade : Window
    {
        private readonly TaskPalisadeViewModel? _viewModel;

        public EditTaskPalisade()
        {
            InitializeComponent();
        }

        public EditTaskPalisade(TaskPalisadeViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = _viewModel ?? (TaskPalisadeViewModel)DataContext;
            // Mettre à jour le mot de passe depuis le PasswordBox
            // Le setter de CalDAVPassword gère automatiquement le chiffrement
            vm.CalDAVPassword = PasswordBox.Password;
            
            vm.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}