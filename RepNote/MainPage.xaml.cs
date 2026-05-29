using Plugin.Maui.Calendar.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
//Réalisé par Ryan Läuppi
namespace RepNote
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly WorkoutService _workoutService;
        private RepNote.Models.WorkoutRoot _loadedData = new();
        public EventCollection Events { get; set; } = new EventCollection();
        public ObservableCollection<EventModel> PlannedDayEvents { get; set; } = new();
        public ObservableCollection<EventModel> AddedDayEvents { get; set; } = new();

        public bool IsPlannedSectionVisible => PlannedDayEvents.Count > 0;
        public bool IsAddedSectionVisible => AddedDayEvents.Count > 0;

        private string _durationText;
        public string DurationText
        {
            get => _durationText;
            set { _durationText = value; OnPropertyChanged(); }
        }
        public bool IsDurationVisible => !string.IsNullOrEmpty(_durationText);

        private DateTime? _selectedDate = DateTime.Now;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                UpdateSelectedDayEvents();
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Initialise le service et le binding context
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            _workoutService = new WorkoutService();
            BindingContext = this;
        }

        /// <summary>
        /// Réinitialise la date sélectionnée et charge les événements
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SelectedDate = DateTime.Today;
            await LoadEventsAsync();
        }

        /// <summary>
        /// Charge tous les workouts et alimente le calendrier
        /// </summary>
        private async Task LoadEventsAsync()
        {
            var data = await _workoutService.LoadWorkoutsAsync();
            _loadedData = data;
            var newEvents = new EventCollection();

            foreach (var group in data.Workouts.GroupBy(w => w.Date.Date))
            {
                var eventList = new List<EventModel>();
                foreach (var workout in group)
                    foreach (var exercise in workout.Exercises)
                        eventList.Add(new EventModel
                        {
                            Name = exercise.Name,
                            Description = $"Séance #{workout.Id} - {workout.Status}",
                            IsPlanned = exercise.IsPlanned
                        });

                if (eventList.Any())
                    newEvents.Add(group.Key, eventList);
            }

            Events = newEvents;
            OnPropertyChanged(nameof(Events));
            UpdateSelectedDayEvents();
        }

        /// <summary>
        /// Met à jour les listes d'exercices et la durée pour le jour sélectionné
        /// </summary>
        private void UpdateSelectedDayEvents()
        {
            PlannedDayEvents.Clear();
            AddedDayEvents.Clear();
            DurationText = null;

            if (_selectedDate == null) return;

            var key = Events.Keys.FirstOrDefault(k => k.Date == _selectedDate.Value.Date);
            if (key != default && Events[key] is List<EventModel> list)
                foreach (var ev in list)
                {
                    if (ev.IsPlanned)
                        PlannedDayEvents.Add(ev);
                    else
                        AddedDayEvents.Add(ev);
                }

            var totalSeconds = _loadedData.Workouts
                .Where(w => w.Date.Date == _selectedDate.Value.Date)
                .Sum(w => w.DurationSeconds);

            if (totalSeconds > 0)
            {
                var duration = TimeSpan.FromSeconds(totalSeconds);
                DurationText = duration.TotalHours >= 1
                    ? $"Durée : {duration:hh\\:mm\\:ss}"
                    : $"Durée : {duration:mm\\:ss}";
            }

            OnPropertyChanged(nameof(IsPlannedSectionVisible));
            OnPropertyChanged(nameof(IsAddedSectionVisible));
            OnPropertyChanged(nameof(IsDurationVisible));
        }

        /// <summary>
        /// Lance la séance en cours
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void onClickButton1(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("SeanceEnCours");

        /// <summary>
        /// Ouvre la planification pour la date sélectionnée
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void onClickButton2(object sender, EventArgs e)
        {
            var date = SelectedDate ?? DateTime.Now;
            await Shell.Current.GoToAsync($"PlanifSerie?date={date:yyyy-MM-dd}");
        }
    }

    /// <summary>
    /// DTO pour l'affichage d'un exercice dans le calendrier
    /// </summary>
    public class EventModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsPlanned { get; set; }
    }
}
