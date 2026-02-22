using System.Windows;

namespace Palisades.View
{
    public partial class CreateTaskPalisadeDialog : Window
    {
        public string PalisadeTitle { get; set; } = "Task Palisade";
        public string CalDAVUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Pour Zimbra OVH : typiquement "Tasks".</summary>
        public string TaskListId { get; set; } = "Tasks";

        public CreateTaskPalisadeDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;

            // Lire aussi les valeurs des TextBox manuellement pour être sûr
            // (au cas où le binding WPF n'aurait pas mis à jour les propriétés)
            PalisadeTitle = PalisadeTitleTextBox.Text;
            CalDAVUrl = CalDAVUrlTextBox.Text;
            Username = UsernameTextBox.Text;
            TaskListId = TaskListIdTextBox.Text;

            if (string.IsNullOrWhiteSpace(CalDAVUrl) || string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("CalDAV URL and Username are required.", "Task Palisade",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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