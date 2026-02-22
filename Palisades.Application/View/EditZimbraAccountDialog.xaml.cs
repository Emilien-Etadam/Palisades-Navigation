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
            Account.CalDAVBaseUrl = CalDAVUrlTextBox.Text?.Trim() ?? "";
            try
            {
                var uri = new System.Uri(Account.CalDAVBaseUrl);
                Account.Server = uri.Host;
            }
            catch
            {
                Account.Server = "";
            }
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
