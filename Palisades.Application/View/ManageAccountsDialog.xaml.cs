using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Palisades.View
{
    public partial class ManageAccountsDialog : Window
    {
        private List<ZimbraAccount> _accounts = new List<ZimbraAccount>();

        public ManageAccountsDialog()
        {
            InitializeComponent();
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            _accounts = ZimbraAccountStore.Load();
            AccountsListBox.ItemsSource = null;
            AccountsListBox.ItemsSource = _accounts;
        }

        private void SaveAccounts()
        {
            ZimbraAccountStore.Save(_accounts);
        }

        private void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = AccountsListBox.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            TestButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditZimbraAccountDialog();
            if (dialog.ShowDialog() == true && dialog.Account != null)
            {
                _accounts.Add(dialog.Account);
                SaveAccounts();
                LoadAccounts();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListBox.SelectedItem is not ZimbraAccount acc) return;
            var dialog = new EditZimbraAccountDialog(acc);
            if (dialog.ShowDialog() == true && dialog.Account != null)
            {
                SaveAccounts();
                LoadAccounts();
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListBox.SelectedItem is not ZimbraAccount acc) return;
            var password = CredentialEncryptor.Decrypt(acc.EncryptedPassword ?? "");
            acc.LastTestStatus = "Testing...";
            AccountsListBox.ItemsSource = null;
            AccountsListBox.ItemsSource = _accounts;

            try
            {
                // Test CalDAV
                if (!string.IsNullOrEmpty(acc.CalDAVBaseUrl))
                {
                    var caldav = new Services.CalDAVService(acc.CalDAVBaseUrl, acc.Email, password);
                    await caldav.GetTaskListsAsync();
                }
                // Test IMAP if host set
                if (!string.IsNullOrEmpty(acc.ImapHost) || !string.IsNullOrEmpty(acc.Server))
                {
                    var host = !string.IsNullOrEmpty(acc.ImapHost) ? acc.ImapHost : acc.Server;
                    var imap = new ImapMailService(host, 993, acc.Email, password);
                    await imap.ConnectAsync();
                    await imap.DisconnectAsync();
                }
                acc.LastTestStatus = "OK";
            }
            catch (System.Exception ex)
            {
                acc.LastTestStatus = "Failed: " + ex.Message;
            }

            SaveAccounts();
            AccountsListBox.ItemsSource = null;
            AccountsListBox.ItemsSource = _accounts;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListBox.SelectedItem is not ZimbraAccount acc) return;
            if (MessageBox.Show($"Delete account {acc.Email}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _accounts.Remove(acc);
            SaveAccounts();
            LoadAccounts();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
