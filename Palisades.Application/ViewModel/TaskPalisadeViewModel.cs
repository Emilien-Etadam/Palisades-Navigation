using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Palisades.ViewModel
{
    public class TaskPalisadeViewModel : INotifyPropertyChanged
    {
        #region Attributes
        private readonly PalisadeModel _model;
        private readonly CalDAVService _caldavService;
        private volatile bool _shouldSave;
        private CalDAVTask? _selectedTask;
        private string _errorMessage = string.Empty;
        private bool _isSyncing;
        private bool _isLoading;
        private string _syncStatus = "Ready";
        private Timer _syncTimer;
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
                
                // Déchiffrer le mot de passe
                try
                {
                    return CredentialEncryptor.Decrypt(_model.CalDAVPassword, GetEncryptionKey());
                }
                catch
                {
                    return string.Empty;
                }
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    _model.CalDAVPassword = string.Empty;
                }
                else
                {
                    // Chiffrer le mot de passe avant de le sauvegarder
                    _model.CalDAVPassword = CredentialEncryptor.Encrypt(value, GetEncryptionKey());
                }
                OnPropertyChanged();
                Save();
            }
        }
=======
        public string CalDAVPassword
        {
            get { return _model.CalDAVPassword ?? string.Empty; }
            set { _model.CalDAVPassword = value; OnPropertyChanged(); Save(); }
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

        public TaskPalisadeViewModel() : this(new PalisadeModel
        {
            Type = PalisadeType.TaskPalisade,
            Name = "Task Palisade",
            Width = 600,
            Height = 400
        }, new CalDAVService("", "", ""))
        { }

        public TaskPalisadeViewModel(PalisadeModel model, CalDAVService caldavService)
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
            
            // Démarrer le thread de sauvegarde
            Thread saveThread = new Thread(SaveAsync);
            saveThread.IsBackground = true;
            saveThread.Start();
        }
        
        private string GetEncryptionKey()
        {
            // Dans une implémentation complète, cette clé devrait être stockée de manière sécurisée
            // et protégée par un mot de passe principal ou le système d'exploitation
            // Pour cette implémentation, nous utilisons une clé dérivée du nom d'utilisateur
            if (string.IsNullOrEmpty(_model.CalDAVUsername))
                return "DefaultPalisadesKey2024";
                
            return $"Palisades_{_model.CalDAVUsername}_Key";
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
                ErrorMessage = "CalDAV configuration incomplete";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SyncStatus = "Loading tasks...";
                
                var tasks = await _caldavService.GetTasksAsync(TaskListId);
                
                // Effacer et ajouter les nouvelles tâches
                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
                
                SyncStatus = "Tasks loaded successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load tasks: {ex.Message}";
                SyncStatus = "Error loading tasks";
            }
            finally
            {
                IsLoading = false;
            }
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
            {
                return;
            }

            try
            {
                IsSyncing = true;
                SyncStatus = "Syncing with CalDAV...";
                ErrorMessage = string.Empty;
                
                // Synchroniser les tâches locales avec le serveur CalDAV
                await _caldavService.SyncTasksAsync(TaskListId, new List<CalDAVTask>(Tasks));
                
                // Recharger les tâches pour s'assurer que nous avons les dernières données
                await LoadTasksAsync();
                
                SyncStatus = "Sync completed: " + DateTime.Now.ToShortTimeString();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Sync failed: {ex.Message}";
                SyncStatus = "Sync error";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public async Task ForceSyncAsync()
        {
            await SyncWithCalDAVAsync();
        }
        
        private void SaveAsync()
        {
            while (true)
            {
                if (_shouldSave)
                {
                    try
                    {
                        string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
                        PDirectory.EnsureExists(saveDirectory);
                        
                        using (var writer = new System.IO.StreamWriter(System.IO.Path.Combine(saveDirectory, "state.xml")))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(PalisadeModel), new Type[] { typeof(Shortcut), typeof(LnkShortcut), typeof(UrlShortcut), typeof(CalDAVTask), typeof(CalDAVTaskList) });
                            serializer.Serialize(writer, _model);
                        }
                        
                        _shouldSave = false;
                    }
                    catch
                    {
                        // Réessayer au prochain cycle
                    }
                }
                Thread.Sleep(1000);
            }
        }
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

        public ICommand OpenAboutCommand { get; private set; } = new RelayCommand<TaskPalisadeViewModel>((viewModel) =>
        {
            About about = new()
            {
                DataContext = new AboutViewModel()
            };
            try
            {
                about.Owner = PalisadesManager.GetWindow(viewModel.Identifier);
            }
            catch { }
            about.ShowDialog();
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