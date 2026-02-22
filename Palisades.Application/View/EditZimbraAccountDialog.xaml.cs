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
                ServerTextBox.Text = existing.Server ?? "";
                CalDAVUrlTextBox.Text = existing.CalDAVBaseUrl;
                ImapHostTextBox.Text = existing.ImapHost;
            }
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
            Account.ImapHost = ImapHostTextBox.Text?.Trim() ?? "";

            // Auto-complétion CalDAV URL si vide
            if (string.IsNullOrWhiteSpace(Account.CalDAVBaseUrl) && !string.IsNullOrWhiteSpace(Account.Server) && !string.IsNullOrWhiteSpace(Account.Email))
                Account.CalDAVBaseUrl = $"https://{Account.Server}/dav/{Account.Email}/";

            // Auto-complétion IMAP Host si vide
            if (string.IsNullOrWhiteSpace(Account.ImapHost) && !string.IsNullOrWhiteSpace(Account.Server))
                Account.ImapHost = Account.Server;

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
