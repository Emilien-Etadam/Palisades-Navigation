using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Palisades.ViewModel
{
    public class TaskPalisadeViewModel : INotifyPropertyChanged, IPalisadeViewModel
    {
        #region Attributes
        private readonly TaskPalisadeModel _model;
        private readonly CalDAVService _caldavService;
        private volatile bool _shouldSave;
        private CalDAVTask? _selectedTask;
        private string _errorMessage = string.Empty;
        private bool _isSyncing;
        private bool _isLoading;
        private string _syncStatus = "Ready";
        private Timer? _syncTimer;
        private readonly object _saveLock = new object();
        private readonly System.Threading.Timer _saveTimer;
        #endregion

        #region Accessors
        public string Identifier
        {
            get { return _model.Identifier; }
            set { _model.Identifier = value; OnPropertyChanged(); Save(); }
        }

        public string Name
        {
            get { return _model.Name; }
            set { _model.Name = value; OnPropertyChanged(); Save(); }
        }

        public int FenceX
        {
            get { return _model.FenceX; }
            set { _model.FenceX = value; OnPropertyChanged(); Save(); }
        }

        public int FenceY
        {
            get { return _model.FenceY; }
            set { _model.FenceY = value; OnPropertyChanged(); Save(); }
        }

        public int Width
        {
            get { return _model.Width; }
            set { _model.Width = value; OnPropertyChanged(); Save(); }
        }

        public int Height
        {
            get { return _model.Height; }
            set { _model.Height = value; OnPropertyChanged(); Save(); }
        }

        public Color HeaderColor
        {
            get { return _model.HeaderColor; }
            set { _model.HeaderColor = value; OnPropertyChanged(); Save(); }
        }

        public Color BodyColor
        {
            get { return _model.BodyColor; }
            set { _model.BodyColor = value; OnPropertyChanged(); Save(); }
        }

        public SolidColorBrush TitleColor
        {
            get => new(_model.TitleColor);
            set { _model.TitleColor = value.Color; OnPropertyChanged(); Save(); }
        }

        public SolidColorBrush LabelsColor
        {
            get => new(_model.LabelsColor);
            set { _model.LabelsColor = value.Color; OnPropertyChanged(); Save(); }
        }

        public string? GroupId { get => _model.GroupId; set { _model.GroupId = value; OnPropertyChanged(); Save(); } }
        public int TabOrder { get => _model.TabOrder; set { _model.TabOrder = value; OnPropertyChanged(); Save(); } }

        public string CalDAVUrl
        {
            get { return _model.CalDAVUrl ?? string.Empty; }
            set { _model.CalDAVUrl = value; OnPropertyChanged(); Save(); }
        }

        public string CalDAVUsername
        {
            get { return _model.CalDAVUsername ?? string.Empty; }
            set { _model.CalDAVUsername = value; OnPropertyChanged(); Save(); }
        }

        public string CalDAVPassword
        {
            get
            {
                if (string.IsNullOrEmpty(_model.CalDAVPassword))
                    return string.Empty;
                try { return CredentialEncryptor.Decrypt(_model.CalDAVPassword); }
                catch { return string.Empty; }
            }
            set
            {
                _model.CalDAVPassword = string.IsNullOrEmpty(value) ? string.Empty : CredentialEncryptor.Encrypt(value);
                OnPropertyChanged();
                Save();
            }
        }

        public string TaskListId
        {
            get { return _model.TaskListId ?? string.Empty; }
            set { _model.TaskListId = value; OnPropertyChanged(); Save(); }
        }

        public int SyncIntervalMinutes
        {
            get { return _model.SyncIntervalMinutes > 0 ? _model.SyncIntervalMinutes : 5; }
            set { _model.SyncIntervalMinutes = value; OnPropertyChanged(); Save(); }
        }

        public bool EnableLogging
        {
            get { return _model.EnableLogging; }
            set { _model.EnableLogging = value; OnPropertyChanged(); Save(); }
        }

        public bool ShowCompletedTasks
        {
            get { return _model.ShowCompletedTasks; }
            set { _model.ShowCompletedTasks = value; OnPropertyChanged(); Save(); }
        }

        public ObservableCollection<CalDAVTask> Tasks { get; set; } = new ObservableCollection<CalDAVTask>();

        public CalDAVTask? SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set { _isSyncing = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SyncStatus
        {
            get => _syncStatus;
            set { _syncStatus = value; OnPropertyChanged(); }
        }
        #endregion

        public TaskPalisadeViewModel() : this(new TaskPalisadeModel { Name = "Task Palisade", Width = 600, Height = 400 }, new CalDAVService(new CalDAVClient("https://localhost/", "", "")))
        { }

        public TaskPalisadeViewModel(TaskPalisadeModel model, CalDAVService caldavService)
        {
            _model = model;
            _caldavService = caldavService;
            
            // Initialiser les collections et événements
            Tasks.CollectionChanged += Tasks_CollectionChanged;
            
            // Charger les tâches si des informations CalDAV sont disponibles
            if (!string.IsNullOrEmpty(_model.CalDAVUrl) && !string.IsNullOrEmpty(_model.TaskListId))
            {
                _ = LoadTasksAsync();
            }
            
            // Démarrer le timer de synchronisation
            StartSyncTimer();
            
            _saveTimer = new System.Threading.Timer(_ => SaveAsync(), null, 1000, 1000);
        }
        
        #region Methods
        public void Save()
        {
            _shouldSave = true;
        }

        public void Delete()
        {
            string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
            if (System.IO.Directory.Exists(saveDirectory))
            {
                System.IO.Directory.Delete(saveDirectory, true);
            }
        }

        public async Task LoadTasksAsync()
        {
            if (string.IsNullOrEmpty(TaskListId) || string.IsNullOrEmpty(CalDAVUrl))
            {
                Dispatch(() => { ErrorMessage = "CalDAV configuration incomplete"; });
                return;
            }

            try
            {
                Dispatch(() => { IsLoading = true; ErrorMessage = string.Empty; SyncStatus = "Loading tasks..."; });
                var tasks = await _caldavService.GetTasksAsync(TaskListId);
                Dispatch(() =>
                {
                    Tasks.Clear();
                    foreach (var task in tasks)
                        Tasks.Add(task);
                    SyncStatus = "Tasks loaded successfully";
                });
            }
            catch (Exception ex)
            {
                var msg = $"Failed to load tasks: {ex.Message}";
                Dispatch(() => { ErrorMessage = msg; SyncStatus = "Error loading tasks"; });
            }
            finally
            {
                Dispatch(() => { IsLoading = false; });
            }
        }

        private static void Dispatch(Action action)
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
                action();
            else
                System.Windows.Application.Current?.Dispatcher.Invoke(action);
        }

        private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Save();
            if (!_isSyncing)
            {
                SyncStatus = "Local changes detected, will sync soon...";
            }
        }

        private void StartSyncTimer()
        {
            // Synchroniser selon l'intervalle configuré
            var syncInterval = TimeSpan.FromMinutes(SyncIntervalMinutes);
            _syncTimer = new Timer(async _ =>
            {
                await SyncWithCalDAVAsync();
            }, null, syncInterval, syncInterval);
        }

        public async Task SyncWithCalDAVAsync()
        {
            if (string.IsNullOrEmpty(TaskListId) || string.IsNullOrEmpty(CalDAVUrl))
                return;

            try
            {
                Dispatch(() => { IsSyncing = true; SyncStatus = "Syncing with CalDAV..."; ErrorMessage = string.Empty; });
                var tasksSnapshot = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => new List<CalDAVTask>(Tasks)).Task;
                var merged = await _caldavService.SyncTasksAsync(TaskListId, tasksSnapshot);
                Dispatch(() =>
                {
                    Tasks.Clear();
                    foreach (var t in merged)
                        Tasks.Add(t);
                    SyncStatus = "Sync completed: " + DateTime.Now.ToShortTimeString();
                });
            }
            catch (Exception ex)
            {
                var msg = $"Sync failed: {ex.Message}";
                Dispatch(() => { ErrorMessage = msg; SyncStatus = "Sync error"; });
            }
            finally
            {
                Dispatch(() => { IsSyncing = false; });
            }
        }

        public async Task ForceSyncAsync()
        {
            await SyncWithCalDAVAsync();
        }
        
        private void SaveAsync()
        {
            if (!_shouldSave) return;
            lock (_saveLock)
            {
                if (!_shouldSave) return;
                try
                {
                    string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
                    PDirectory.EnsureExists(saveDirectory);
                    using var writer = new System.IO.StreamWriter(System.IO.Path.Combine(saveDirectory, "state.xml"));
                    ViewModelBase.SharedSerializer.Serialize(writer, _model);
                }
                catch { /* réessayer au prochain cycle */ }
                finally { _shouldSave = false; }
            }
        }

        public ObservableCollection<LayoutSnapshot> RecentSnapshots { get; } = new();

        public void RefreshRecentSnapshots()
        {
            RecentSnapshots.Clear();
            foreach (var s in LayoutSnapshotService.ListSnapshots().Take(5))
                RecentSnapshots.Add(s);
        }

        public ICommand SaveSnapshotCommand { get; private set; } = new RelayCommand(() =>
        {
            var dialog = new View.SaveSnapshotDialog();
            try { dialog.Owner = Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SnapshotName))
                LayoutSnapshotService.SaveSnapshot(dialog.SnapshotName.Trim());
        });

        public ICommand ManageSnapshotsCommand { get; private set; } = new RelayCommand(() =>
        {
            var dialog = new View.ManageSnapshotsDialog();
            try { dialog.Owner = Application.Current.MainWindow; } catch { }
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
        #endregion

        #region Commands
        public ICommand NewPalisadeCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.CreatePalisade();
        });

        public ICommand NewFolderPortalCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.ShowCreateFolderPortalDialog();
        });

        public ICommand NewTaskPalisadeCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.ShowCreateTaskPalisadeDialog();
        });

        public ICommand NewCalendarPalisadeCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.ShowCreateCalendarPalisadeDialog();
        });

        public ICommand NewMailPalisadeCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.ShowCreateMailPalisadeDialog();
        });

        public ICommand ManageZimbraAccountsCommand { get; private set; } = new RelayCommand(() => { var d = new View.ManageAccountsDialog(); d.ShowDialog(); });

        public ICommand DeletePalisadeCommand { get; private set; } = new RelayCommand<string>((identifier) => PalisadesManager.DeletePalisade(identifier));

        public ICommand EditTaskPalisadeCommand { get; private set; } = new RelayCommand<TaskPalisadeViewModel>((viewModel) =>
        {
            EditTaskPalisade edit = new()
            {
                DataContext = viewModel
            };
            try
            {
                edit.Owner = PalisadesManager.GetWindow(viewModel.Identifier);
            }
            catch { }
            edit.ShowDialog();
        });

        public ICommand ShowSettingsCommand { get; private set; } = new RelayCommand<TaskPalisadeViewModel>((viewModel) =>
        {
            TaskPalisadeSettingsDialog settings = new()
            {
                DataContext = viewModel
            };
            try
            {
                settings.Owner = PalisadesManager.GetWindow(viewModel.Identifier);
            }
            catch { }
            settings.ShowDialog();
        });

        public ICommand ForceSyncCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await ForceSyncAsync();
                });
            }
        }

        public ICommand AddTaskCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var newTask = new CalDAVTask("New Task")
                    {
                        Description = "Task description",
                        DueDate = DateTime.Today.AddDays(1)
                    };
                    Tasks.Add(newTask);
                    SelectedTask = newTask;
                });
            }
        }

        public ICommand DeleteTaskCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (SelectedTask == null) return;
                    
                    try
                    {
                        // Supprimer du serveur CalDAV si l'ID CalDAV existe
                        if (!string.IsNullOrEmpty(SelectedTask.CalDAVId))
                        {
                            await _caldavService.DeleteTaskAsync(TaskListId, SelectedTask.CalDAVId);
                        }
                        
                        // Supprimer de la collection locale
                        Tasks.Remove(SelectedTask);
                        SelectedTask = null;
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Failed to delete task: {ex.Message}";
                    }
                });
            }
        }

        public ICommand ToggleTaskCompletedCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (SelectedTask == null) return;
                    
                    SelectedTask.Completed = !SelectedTask.Completed;
                    SelectedTask.CompletedDate = SelectedTask.Completed ? DateTime.Now : null;
                    SelectedTask.LastModified = DateTime.Now;
                    
                    try
                    {
                        // Mettre à jour sur le serveur CalDAV si l'ID CalDAV existe
                        if (!string.IsNullOrEmpty(SelectedTask.CalDAVId))
                        {
                            await _caldavService.UpdateTaskAsync(TaskListId, SelectedTask);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Failed to update task: {ex.Message}";
                        // Revertir le changement local en cas d'échec
                        SelectedTask.Completed = !SelectedTask.Completed;
                        SelectedTask.CompletedDate = SelectedTask.Completed ? DateTime.Now : null;
                    }
                });
            }
        }

        public ICommand SaveTaskCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (SelectedTask == null) return;
                    
                    try
                    {
                        SelectedTask.LastModified = DateTime.Now;
                        
                        // Créer ou mettre à jour sur le serveur CalDAV
                        if (string.IsNullOrEmpty(SelectedTask.CalDAVId))
                        {
                            // Nouvelle tâche - créer sur le serveur
                            var createdTask = await _caldavService.CreateTaskAsync(TaskListId, SelectedTask);
                            SelectedTask.CalDAVId = createdTask.CalDAVId;
                            SelectedTask.CalDAVEtag = createdTask.CalDAVEtag;
                        }
                        else
                        {
                            // Tâche existante - mettre à jour sur le serveur
                            await _caldavService.UpdateTaskAsync(TaskListId, SelectedTask);
                        }
                        
                        SyncStatus = "Task saved successfully";
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Failed to save task: {ex.Message}";
                    }
                });
            }
        }
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}