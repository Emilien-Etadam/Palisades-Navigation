using Palisades.Model;
using Palisades.ViewModel;
using System.Windows;

namespace Palisades.View
{
    public partial class EditCalendarPalisade : Window
    {
        public EditCalendarPalisade()
        {
            InitializeComponent();
        }

        public EditCalendarPalisade(CalendarPalisadeViewModel viewModel) : this()
        {
            DataContext = viewModel;
            Loaded += (s, _) =>
            {
                ViewModeCombo.ItemsSource = new[] { CalendarViewMode.Agenda, CalendarViewMode.Day, CalendarViewMode.Week };
                if (DataContext is CalendarPalisadeViewModel vm)
                    ViewModeCombo.SelectedItem = vm.ViewMode;
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CalendarPalisadeViewModel vm)
            {
                if (ViewModeCombo.SelectedItem is CalendarViewMode mode)
                    vm.ViewMode = mode;
                vm.Save();
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
