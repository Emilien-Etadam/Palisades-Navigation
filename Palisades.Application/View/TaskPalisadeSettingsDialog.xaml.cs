using Palisades.ViewModel;
using System.Windows;

namespace Palisades.View
{
    public partial class TaskPalisadeSettingsDialog : Window
    {
        private readonly TaskPalisadeViewModel? _viewModel;

        public TaskPalisadeSettingsDialog()
        {
            InitializeComponent();
        }

        public TaskPalisadeSettingsDialog(TaskPalisadeViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // Initialiser les contrôles avec les valeurs actuelles
            SyncIntervalSlider.Value = 5; // Valeur par défaut
            EnableLoggingCheckBox.IsChecked = false; // Désactivé par défaut
            ShowCompletedCheckBox.IsChecked = true; // Activé par défaut
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Sauvegarder les paramètres
            // Dans une implémentation complète, ces paramètres seraient sauvegardés
            // et utilisés pour configurer le comportement de la Task Palisade
            
            var syncInterval = (int)SyncIntervalSlider.Value;
            var enableLogging = EnableLoggingCheckBox.IsChecked ?? false;
            var showCompleted = ShowCompletedCheckBox.IsChecked ?? true;
            
            // Appliquer les paramètres
            // _viewModel.ApplySettings(syncInterval, enableLogging, showCompleted);
            
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