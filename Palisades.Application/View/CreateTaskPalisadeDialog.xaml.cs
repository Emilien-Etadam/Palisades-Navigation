using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Palisades.Services;

namespace Palisades.View
{
    public partial class CreateTaskPalisadeDialog : Window
    {
        public string PalisadeTitle { get; set; } = "Task Palisade";
        public string CalDAVUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> SelectedTaskListIds { get; private set; } = new();

        private List<CalDAVCalendarInfo>? _taskLists;

        public CreateTaskPalisadeDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private async void LoadListsButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            var url = CalDAVUrlTextBox.Text?.Trim() ?? "";
            var user = UsernameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(user))
            {
                MessageBox.Show("Please enter CalDAV URL and username.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The CalDAV URL must start with https://.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var client = new CalDAVClient(url, user, Password);
                var allCalendars = await client.DiscoverCalendarsAsync();
                _taskLists = allCalendars
                    .Where(c => c.SupportedComponents.Contains("VTODO", StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (_taskLists.Count == 0)
                {
                    MessageBox.Show("No task list (VTODO) found at this URL.", "Task Lists", MessageBoxButton.OK, MessageBoxImage.Information);
                    TaskListsListBox.ItemsSource = null;
                    return;
                }

                TaskListsListBox.ItemsSource = _taskLists;
                TaskListsListBox.SelectedItems.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load task lists: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            PalisadeTitle = PalisadeTitleTextBox.Text;
            CalDAVUrl = CalDAVUrlTextBox.Text?.Trim() ?? "";
            Username = UsernameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(CalDAVUrl))
            {
                MessageBox.Show("Please enter a CalDAV Server URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!CalDAVUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The CalDAV URL must start with https://.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Please enter a username.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedTaskListIds = TaskListsListBox.SelectedItems
                .Cast<CalDAVCalendarInfo>()
                .Select(c => c.Href)
                .ToList();

            if (SelectedTaskListIds.Count == 0 && _taskLists != null && _taskLists.Count > 0)
            {
                MessageBox.Show("Please select at least one task list.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
