using Palisades.ViewModel;
using System.Windows;
using System.Windows.Media;

namespace Palisades.View
{
    public partial class EditTaskPalisade : Window
    {
        private readonly TaskPalisadeViewModel _viewModel;

        public EditTaskPalisade()
        {
            InitializeComponent();
        }

        public EditTaskPalisade(TaskPalisadeViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private void ChangeHeaderColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new PixiEditor.ColorPicker.ColorPickerDialog();
            colorPicker.StartingColor = _viewModel.HeaderColor;
            
            if (colorPicker.ShowDialog() == true)
            {
                _viewModel.HeaderColor = colorPicker.SelectedColor;
            }
        }

        private void ChangeBodyColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new PixiEditor.ColorPicker.ColorPickerDialog();
            colorPicker.StartingColor = _viewModel.BodyColor;
            
            if (colorPicker.ShowDialog() == true)
            {
                _viewModel.BodyColor = colorPicker.SelectedColor;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour le mot de passe depuis le PasswordBox
            // Le setter de CalDAVPassword gère automatiquement le chiffrement
            _viewModel.CalDAVPassword = PasswordBox.Password;
            
            _viewModel.Save();
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