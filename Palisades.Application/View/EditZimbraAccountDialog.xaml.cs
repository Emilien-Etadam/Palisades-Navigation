using Palisades.Helpers;
using Palisades.Model;
using System.Windows;
using System.Windows.Controls;

namespace Palisades.View
{
    public partial class EditZimbraAccountDialog : Window
    {
        public ZimbraAccount? Account { get; private set; }

        public EditZimbraAccountDialog(ZimbraAccount? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                Account = existing;
                EmailTextBox.Text = existing.Email;
                ServerTextBox.Text = existing.Server;
                CalDAVUrlTextBox.Text = existing.CalDAVBaseUrl;
                ImapHostTextBox.Text = existing.ImapHost;
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        private void DetectOvhButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text?.Trim() ?? "";
            var (imapHost, caldavBaseUrl) = ZimbraOvhDetection.SuggestFromEmail(email);
            ImapHostTextBox.Text = imapHost;
            CalDAVUrlTextBox.Text = caldavBaseUrl;
            try
            {
                var uri = new System.Uri(caldavBaseUrl);
                ServerTextBox.Text = uri.Host;
            }
            catch { ServerTextBox.Text = "ssl0.ovh.net"; }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Email is required.", "Account", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Account == null)
                Account = new ZimbraAccount();
            Account.Email = email;
            Account.Server = ServerTextBox.Text?.Trim() ?? "";
            Account.CalDAVBaseUrl = CalDAVUrlTextBox.Text?.Trim() ?? "";
            Account.ImapHost = ImapHostTextBox.Text?.Trim() ?? Account.Server;
            if (!string.IsNullOrEmpty(PasswordBox.Password))
                Account.EncryptedPassword = CredentialEncryptor.Encrypt(PasswordBox.Password);
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
