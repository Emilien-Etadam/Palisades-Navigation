using System;
using System.Windows;

namespace Palisades.View
{
    public partial class SaveSnapshotDialog : Window
    {
        public string SnapshotName { get; private set; } = "";

        public SaveSnapshotDialog()
        {
            InitializeComponent();
            NameTextBox.Text = "Layout - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            NameTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a name.", "Save layout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SnapshotName = name;
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
