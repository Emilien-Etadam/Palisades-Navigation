using System.Collections.ObjectModel;
using System.Linq;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Palisades.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IPalisadeViewModel, IDisposable
    {
        /// <summary>Sérialiseur partagé pour garantir un format XML compatible avec LoadPalisades (root/namespace cohérents).</summary>
        public static readonly XmlSerializer SharedSerializer = new XmlSerializer(
            typeof(PalisadeModelBase),
            new[]
            {
                typeof(PalisadeModel),
                typeof(StandardPalisadeModel),
                typeof(FolderPortalModel),
                typeof(TaskPalisadeModel),
                typeof(CalendarPalisadeModel),
                typeof(MailPalisadeModel),
                typeof(Shortcut),
                typeof(LnkShortcut),
                typeof(UrlShortcut)
            });

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

        public string? GroupId { get => Model.GroupId; set { Model.GroupId = value; OnPropertyChanged(); Save(); } }
        public int TabOrder { get => Model.TabOrder; set { Model.TabOrder = value; OnPropertyChanged(); Save(); } }
        public PalisadeModelBase ModelBase => Model;

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
                    SharedSerializer.Serialize(writer, Model);
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

        public ICommand OpenAboutCommand { get; } = new RelayCommand(() =>
        {
            var about = new About
            {
                DataContext = new AboutViewModel()
            };
            try { about.Owner = System.Windows.Application.Current.MainWindow; } catch { }
            about.ShowDialog();
        });

        public ICommand SaveSnapshotCommand { get; } = new RelayCommand(() =>
        {
            var dialog = new SaveSnapshotDialog();
            try { dialog.Owner = System.Windows.Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SnapshotName))
                LayoutSnapshotService.SaveSnapshot(dialog.SnapshotName.Trim());
        });

        public ICommand ManageSnapshotsCommand { get; } = new RelayCommand(() =>
        {
            var dialog = new ManageSnapshotsDialog();
            try { dialog.Owner = System.Windows.Application.Current.MainWindow; } catch { }
            dialog.ShowDialog();
        });

        public ICommand RestoreSnapshotCommand { get; } = new RelayCommand<string>(id =>
        {
            if (string.IsNullOrEmpty(id)) return;
            if (System.Windows.MessageBox.Show("This will replace your current layout. Continue?", "Restore layout",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            LayoutSnapshotService.RestoreSnapshot(id);
        });

        public ObservableCollection<LayoutSnapshot> RecentSnapshots { get; } = new();

        public void RefreshRecentSnapshots()
        {
            RecentSnapshots.Clear();
            foreach (var s in LayoutSnapshotService.ListSnapshots().Take(5))
                RecentSnapshots.Add(s);
        }

        #endregion

        public virtual void Dispose()
        {
            _saveTimer.Dispose();
            if (ShouldSave) SaveAsync();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
