using System.Windows;

namespace Palisades.View
{
    public partial class CreateTaskPalisadeDialog : Window
    {
        public string PalisadeTitle { get; set; } = "Task Palisade";
        public string CalDAVUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string TaskListId { get; set; } = string.Empty;

        public CreateTaskPalisadeDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Mettre à jour le mot de passe depuis le PasswordBox
            Password = PasswordBox.Password;
            
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