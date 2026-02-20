using Palisades.Helpers;
using Palisades.Model;
using Palisades.View;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;

namespace Palisades.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected readonly PalisadeModelBase Model;
        protected volatile bool ShouldSave;
        private readonly object _saveLock = new object();
        private readonly System.Threading.Timer _saveTimer;

        protected ViewModelBase(PalisadeModelBase model)
        {
            Model = model;
            _saveTimer = new System.Threading.Timer(_ => SaveAsync(), null, 1000, 1000);
        }

        #region Propriétés communes

        public string Identifier
        {
            get => Model.Identifier;
            set { Model.Identifier = value; OnPropertyChanged(); Save(); }
        }

        public string Name
        {
            get => Model.Name;
            set { Model.Name = value; OnPropertyChanged(); Save(); }
        }

        public int FenceX
        {
            get => Model.FenceX;
            set { Model.FenceX = value; OnPropertyChanged(); Save(); }
        }

        public int FenceY
        {
            get => Model.FenceY;
            set { Model.FenceY = value; OnPropertyChanged(); Save(); }
        }

        public int Width
        {
            get => Model.Width;
            set { Model.Width = value; OnPropertyChanged(); Save(); }
        }

        public int Height
        {
            get => Model.Height;
            set { Model.Height = value; OnPropertyChanged(); Save(); }
        }

        public Color HeaderColor
        {
            get => Model.HeaderColor;
            set { Model.HeaderColor = value; OnPropertyChanged(); Save(); }
        }

        public Color BodyColor
        {
            get => Model.BodyColor;
            set { Model.BodyColor = value; OnPropertyChanged(); Save(); }
        }

        private SolidColorBrush? _titleColorBrush;
        private SolidColorBrush? _labelsColorBrush;

        public SolidColorBrush TitleColor
        {
            get
            {
                if (_titleColorBrush == null || _titleColorBrush.Color != Model.TitleColor)
                    _titleColorBrush = new SolidColorBrush(Model.TitleColor);
                return _titleColorBrush;
            }
            set { Model.TitleColor = value.Color; _titleColorBrush = value; OnPropertyChanged(); Save(); }
        }

        public SolidColorBrush LabelsColor
        {
            get
            {
                if (_labelsColorBrush == null || _labelsColorBrush.Color != Model.LabelsColor)
                    _labelsColorBrush = new SolidColorBrush(Model.LabelsColor);
                return _labelsColorBrush;
            }
            set { Model.LabelsColor = value.Color; _labelsColorBrush = value; OnPropertyChanged(); Save(); }
        }

        #endregion

        #region Méthodes communes

        public void Save()
        {
            ShouldSave = true;
        }

        public void Delete()
        {
            string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, true);
            }
        }

        /// <summary>
        /// Sérialise le modèle dans le flux XML. Chaque sous-classe implémente pour ses types spécifiques.
        /// </summary>
        protected abstract void SerializeModel(StreamWriter writer);

        /// <summary>
        /// Callback du timer : sauvegarde toutes les 1 s si nécessaire.
        /// </summary>
        private void SaveAsync()
        {
            if (!ShouldSave) return;
            lock (_saveLock)
            {
                if (!ShouldSave) return;
                try
                {
                    string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
                    PDirectory.EnsureExists(saveDirectory);
                    using StreamWriter writer = new(Path.Combine(saveDirectory, "state.xml"));
                    SerializeModel(writer);
                }
                catch { /* réessayer au prochain cycle */ }
                finally { ShouldSave = false; }
            }
        }

        #endregion

        #region Commandes communes

        public ICommand NewPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.CreatePalisade());

        public ICommand NewFolderPortalCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateFolderPortalDialog());

        public ICommand NewTaskPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateTaskPalisadeDialog());

        public ICommand NewCalendarPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateCalendarPalisadeDialog());

        public ICommand NewMailPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateMailPalisadeDialog());

        public ICommand ManageZimbraAccountsCommand { get; } = new RelayCommand(() =>
        {
            var dialog = new ManageAccountsDialog();
            dialog.ShowDialog();
        });

        public ICommand DeletePalisadeCommand { get; } = new RelayCommand<string>(identifier => PalisadesManager.DeletePalisade(identifier));

        public ICommand OpenAboutCommand { get; } = new RelayCommand<ViewModelBase>(viewModel =>
        {
            if (viewModel == null) return;
            var about = new About
            {
                DataContext = new AboutViewModel()
            };
            try { about.Owner = PalisadesManager.GetWindow(viewModel.Identifier); } catch { }
            about.ShowDialog();
        });

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
