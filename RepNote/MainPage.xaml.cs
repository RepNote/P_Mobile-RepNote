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
        public EventCollection Events { get; set; } = new EventCollection();
        public ObservableCollection<EventModel> SelectedDayEvents { get; set; } = new();

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

        public MainPage()
        {
            InitializeComponent();
            _workoutService = new WorkoutService();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            var data = await _workoutService.LoadWorkoutsAsync();
            Events.Clear();

            foreach (var group in data.Workouts.GroupBy(w => w.Date.Date))
            {
                var eventList = new List<EventModel>();
                foreach (var workout in group)
                    foreach (var exercise in workout.Exercises)
                        eventList.Add(new EventModel
                        {
                            Name = exercise.Name,
                            Description = $"Séance #{workout.Id} – {workout.Status}"
                        });

                Events.Add(group.Key, eventList);
            }

            UpdateSelectedDayEvents();
        }

        private void UpdateSelectedDayEvents()
        {
            SelectedDayEvents.Clear();
            if (_selectedDate == null) return;

            var key = Events.Keys.FirstOrDefault(k => k.Date == _selectedDate.Value.Date);
            if (key != default && Events[key] is List<EventModel> list)
                foreach (var ev in list)
                    SelectedDayEvents.Add(ev);
        }

        public async void onClickButton1(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("SeanceEnCours");

        public async void onClickButton2(object sender, EventArgs e)
        {
            var date = SelectedDate ?? DateTime.Now;
            await Shell.Current.GoToAsync($"PlanifSerie?date={date:yyyy-MM-dd}");
        }
    }

    public class EventModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}