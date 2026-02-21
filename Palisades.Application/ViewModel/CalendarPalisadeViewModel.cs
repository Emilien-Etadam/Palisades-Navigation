using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Palisades.ViewModel
{
    public class CalendarPalisadeViewModel : ViewModelBase
    {
        private readonly CalendarPalisadeModel _model;
        private readonly CalendarCalDAVService _calendarService;
        private DateTime _selectedDate = DateTime.Today;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private Timer? _refreshTimer;

        public CalendarPalisadeViewModel() : this(
            new CalendarPalisadeModel { Name = "Calendar", Width = 500, Height = 400 },
            new CalendarCalDAVService("", "", ""))
        { }

        public CalendarPalisadeViewModel(CalendarPalisadeModel model, CalendarCalDAVService calendarService)
            : base(model)
        {
            _model = model;
            _calendarService = calendarService;
            Events = new ObservableCollection<CalendarEvent>();
            _ = LoadEventsAsync();
            StartRefreshTimer();
        }

        public ObservableCollection<CalendarEvent> Events { get; }

        public CalendarViewMode ViewMode
        {
            get => _model.ViewMode;
            set { _model.ViewMode = value; OnPropertyChanged(); Save(); _ = LoadEventsAsync(); }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(); _ = LoadEventsAsync(); }
        }

        public int DaysToShow
        {
            get => _model.DaysToShow;
            set { _model.DaysToShow = value; OnPropertyChanged(); Save(); _ = LoadEventsAsync(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public async Task LoadEventsAsync()
        {
            if (_model.CalendarIds == null || _model.CalendarIds.Count == 0)
            {
                Dispatch(() => { Events.Clear(); ErrorMessage = ""; });
                return;
            }
            IsLoading = true;
            ErrorMessage = "";
            try
            {
                var start = SelectedDate.Date;
                var end = start.AddDays(DaysToShow);
                var allEvents = new List<CalendarEvent>();
                foreach (var calId in _model.CalendarIds)
                {
                    var list = await _calendarService.GetEventsAsync(calId, start, end);
                    allEvents.AddRange(list);
                }
                var ordered = allEvents.OrderBy(e => e.DtStart).ToList();
                Dispatch(() =>
                {
                    Events.Clear();
                    foreach (var evt in ordered)
                        Events.Add(evt);
                });
            }
            catch (Exception ex)
            {
                Dispatch(() => ErrorMessage = ex.Message);
            }
            finally
            {
                Dispatch(() => IsLoading = false);
            }
        }

        private void StartRefreshTimer()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = new Timer(async _ => await LoadEventsAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private static void Dispatch(Action action)
        {
            if (Application.Current?.Dispatcher != null)
                Application.Current.Dispatcher.BeginInvoke(action);
            else
                action();
        }

        protected override void SerializeModel(StreamWriter writer)
        {
            var serializer = new XmlSerializer(typeof(CalendarPalisadeModel));
            serializer.Serialize(writer, _model);
        }

        #region Commands

        public ICommand NewPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.CreatePalisade());
        public ICommand NewFolderPortalCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateFolderPortalDialog());
        public ICommand NewTaskPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateTaskPalisadeDialog());
        public ICommand NewCalendarPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateCalendarPalisadeDialog());
        public ICommand NewMailPalisadeCommand { get; } = new RelayCommand(() => PalisadesManager.ShowCreateMailPalisadeDialog());
        public ICommand DeletePalisadeCommand { get; } = new RelayCommand<string>(id => PalisadesManager.DeletePalisade(id));
        public ICommand OpenAboutCommand { get; } = new RelayCommand<ViewModelBase>(vm =>
        {
            if (vm == null) return;
            var about = new About { DataContext = new AboutViewModel() };
            try { about.Owner = PalisadesManager.GetWindow(vm.Identifier); } catch { }
            about.ShowDialog();
        });
        public ICommand RefreshCommand { get; } = new RelayCommand<CalendarPalisadeViewModel>(async vm => { if (vm != null) await vm.LoadEventsAsync(); });

        public ICommand EditCalendarPalisadeCommand { get; } = new RelayCommand<CalendarPalisadeViewModel>(vm =>
        {
            if (vm == null) return;
            // TODO Phase 5.6+: EditCalendarPalisadeDialog
        });

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
