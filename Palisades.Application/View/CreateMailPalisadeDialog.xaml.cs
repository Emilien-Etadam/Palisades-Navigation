using Palisades.Model;
using Palisades.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Palisades.View
{
    public partial class CreateMailPalisadeDialog : Window
    {
        public string PalisadeTitle { get; set; } = "Mail";
        public string ImapHost { get; set; } = string.Empty;
        public int ImapPort { get; set; } = 993;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public MailDisplayMode DisplayMode { get; set; } = MailDisplayMode.CountOnly;
        public int PollIntervalMinutes { get; set; } = 3;
        public List<string> SelectedFolders { get; private set; } = new List<string>();
        public string? WebmailUrl { get; set; }

        public CreateMailPalisadeDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                Password = pb.Password;
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(ImapHost) || string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Please enter IMAP host and username.", "Mail", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var service = new ImapMailService(ImapHost, ImapPort, Username, Password);
                await service.ConnectAsync();
                var folders = await service.GetFolderNamesAsync();
                FoldersListBox.ItemsSource = folders;
                FoldersListBox.SelectedItems.Clear();
                if (folders.Contains("INBOX"))
                    FoldersListBox.SelectedItems.Add("INBOX");
                await service.DisconnectAsync();
                MessageBox.Show("Connection successful. Select folders to monitor.", "Mail", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Mail", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            DisplayMode = DisplayModeCombo.SelectedIndex == 1 ? MailDisplayMode.CountAndSubjects : MailDisplayMode.CountOnly;
            SelectedFolders = FoldersListBox.SelectedItems.Cast<string>().ToList();
            if (SelectedFolders.Count == 0)
                SelectedFolders.Add("INBOX");
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
