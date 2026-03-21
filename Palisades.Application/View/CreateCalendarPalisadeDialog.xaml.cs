using System;
using System.Collections.Generic;
using Palisades.Model;
using Palisades.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Palisades.View
{
    public partial class CreateCalendarPalisadeDialog : Window
    {
        public string PalisadeTitle { get; set; } = "Calendar palisade";
        public string CalDAVUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> SelectedCalendarIds { get; private set; } = new List<string>();
        public CalendarViewMode ViewMode { get; set; } = CalendarViewMode.Agenda;
        public int DaysToShow { get; set; } = 7;

        private List<CalDAVCalendarInfo>? _calendarList;

        public CreateCalendarPalisadeDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                Password = pb.Password;
        }

        private async void LoadCalendarsButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(CalDAVUrl) || string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Please enter CalDAV URL and username.", "Calendar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                using var client = new CalDAVClient(CalDAVUrl, Username, Password);
                var service = new CalendarCalDAVService(client);
                _calendarList = await service.GetCalendarListAsync();
                CalendarsListBox.ItemsSource = _calendarList;
                CalendarsListBox.SelectedItems.Clear();
                if (_calendarList.Count == 0)
                    MessageBox.Show("No calendar collection found at this URL.", "Calendar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load calendars: {ex.Message}", "Calendar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CalendarsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedCalendarIds = CalendarsListBox.SelectedItems.Cast<CalDAVCalendarInfo>().Select(c => c.Href).ToList();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            if (CalendarsListBox.SelectedItems.Count > 0)
                SelectedCalendarIds = CalendarsListBox.SelectedItems.Cast<CalDAVCalendarInfo>().Select(c => c.Href).ToList();
            var url = CalDAVUrlTextBox.Text?.Trim() ?? "";
            var user = UsernameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Please enter a CalDAV Base URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The CalDAV URL must start with https://.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(user))
            {
                MessageBox.Show("Please enter a username.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
