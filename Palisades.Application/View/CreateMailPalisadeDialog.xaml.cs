using Palisades.Model;
using Palisades.Services;
using System;
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
                folders.Sort(StringComparer.OrdinalIgnoreCase);
                FoldersListBox.ItemsSource = folders;
                FoldersListBox.SelectedItems.Clear();
                if (folders.Contains("INBOX"))
                    FoldersListBox.SelectedItems.Add("INBOX");
                service.Disconnect();
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

            // Lire manuellement tous les champs pour garantir les valeurs même si les bindings WPF ont échoué
            PalisadeTitle = PalisadeTitleTextBox.Text;
            ImapHost = ImapHostTextBox.Text;
            Username = UsernameTextBox.Text;

            var host = ImapHostTextBox.Text?.Trim() ?? "";
            var user = UsernameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Please enter an IMAP Host.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(user))
            {
                MessageBox.Show("Please enter a username.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
