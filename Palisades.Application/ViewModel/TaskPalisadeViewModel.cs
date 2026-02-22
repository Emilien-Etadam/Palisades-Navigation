using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Palisades.ViewModel
{
    public class TaskPalisadeViewModel : ViewModelBase
    {
        private readonly TaskPalisadeModel _model;
        private readonly CalDAVService _caldavService;
        private CalDAVTask? _selectedTask;
        private string _errorMessage = string.Empty;
        private bool _isSyncing;
        private bool _isLoading;
        private string _syncStatus = "Ready";
        private Timer? _syncTimer;

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

        public TaskPalisadeViewModel() : this(new TaskPalisadeModel { Name = "Task Palisade", Width = 600, Height = 400 }, new CalDAVService(new CalDAVClient("https://localhost/", "", "")))
        { }

        public TaskPalisadeViewModel(TaskPalisadeModel model, CalDAVService caldavService) : base(model)
        {
            _model = model;
            _caldavService = caldavService;

            Tasks.CollectionChanged += Tasks_CollectionChanged;

            if (!string.IsNullOrEmpty(_model.CalDAVUrl) && !string.IsNullOrEmpty(_model.TaskListId))
            {
                _ = LoadTasksAsync();
            }

            StartSyncTimer();
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
            if (Application.Current?.Dispatcher.CheckAccess() == true)
                action();
            else
                Application.Current?.Dispatcher.Invoke(action);
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
                var tasksSnapshot = await Application.Current.Dispatcher.InvokeAsync(() => new List<CalDAVTask>(Tasks)).Task;
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

        public ICommand EditTaskPalisadeCommand { get; } = new RelayCommand<TaskPalisadeViewModel>(viewModel =>
        {
            var edit = new EditTaskPalisade { DataContext = viewModel };
            try { edit.Owner = PalisadesManager.GetWindow(viewModel.Identifier); } catch { }
            edit.ShowDialog();
        });

        public ICommand ShowSettingsCommand { get; } = new RelayCommand<TaskPalisadeViewModel>(viewModel =>
        {
            var settings = new TaskPalisadeSettingsDialog { DataContext = viewModel };
            try { settings.Owner = PalisadesManager.GetWindow(viewModel.Identifier); } catch { }
            settings.ShowDialog();
        });

        public ICommand ForceSyncCommand => new RelayCommand(async () => await ForceSyncAsync());

        public ICommand AddTaskCommand => new RelayCommand(() =>
        {
            var newTask = new CalDAVTask("New Task")
            {
                Description = "Task description",
                DueDate = DateTime.Today.AddDays(1)
            };
            Tasks.Add(newTask);
            SelectedTask = newTask;
        });

        public ICommand DeleteTaskCommand => new RelayCommand(async () =>
        {
            if (SelectedTask == null) return;
            try
            {
                if (!string.IsNullOrEmpty(SelectedTask.CalDAVId))
                    await _caldavService.DeleteTaskAsync(TaskListId, SelectedTask.CalDAVId);
                Tasks.Remove(SelectedTask);
                SelectedTask = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete task: {ex.Message}";
            }
        });

        public ICommand ToggleTaskCompletedCommand => new RelayCommand(async () =>
        {
            if (SelectedTask == null) return;
            SelectedTask.Completed = !SelectedTask.Completed;
            SelectedTask.CompletedDate = SelectedTask.Completed ? DateTime.Now : null;
            SelectedTask.LastModified = DateTime.Now;
            try
            {
                if (!string.IsNullOrEmpty(SelectedTask.CalDAVId))
                    await _caldavService.UpdateTaskAsync(TaskListId, SelectedTask);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update task: {ex.Message}";
                SelectedTask.Completed = !SelectedTask.Completed;
                SelectedTask.CompletedDate = SelectedTask.Completed ? DateTime.Now : null;
            }
        });

        public ICommand SaveTaskCommand => new RelayCommand(async () =>
        {
            if (SelectedTask == null) return;
            try
            {
                SelectedTask.LastModified = DateTime.Now;
                if (string.IsNullOrEmpty(SelectedTask.CalDAVId))
                {
                    var createdTask = await _caldavService.CreateTaskAsync(TaskListId, SelectedTask);
                    SelectedTask.CalDAVId = createdTask.CalDAVId;
                    SelectedTask.CalDAVEtag = createdTask.CalDAVEtag;
                }
                else
                {
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
