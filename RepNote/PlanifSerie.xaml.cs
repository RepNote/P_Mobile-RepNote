using RepNote.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
using System.Globalization;
//Réalisé par Ryan Läuppi (Ryancmoi)
namespace RepNote;

[QueryProperty(nameof(SelectedDateString), "date")]
public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> MyWorkouts { get; set; } = new();
    private readonly WorkoutService _workoutService;
    private DateTime _selectedDate = DateTime.Now;

    // Reçu depuis MainPage via Shell navigation
    public string SelectedDateString
    {
        set
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                _selectedDate = d;
            }
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadExistingWorkoutsAsync();
    }

    private async Task LoadExistingWorkoutsAsync()
    {
        var data = await _workoutService.LoadWorkoutsAsync();

        var existing = data.Workouts
            .FirstOrDefault(w => w.Date.Date == _selectedDate.Date);

        if (existing != null)
        {
            MyWorkouts.Clear();
            foreach (var exercise in existing.Exercises)
            {
                MyWorkouts.Add(exercise.Name);
            }
        }
    }

    public PlanifSerie()
    {
        InitializeComponent();
        _workoutService = new WorkoutService();
        SeriesList.ItemsSource = MyWorkouts;
    }

    public void OnAddSeriesClicked(object sender, EventArgs e)
    {
        string workoutName = SeriesEntry.Text;
        if (!string.IsNullOrEmpty(workoutName))
        {
            MyWorkouts.Add(workoutName);
            SeriesEntry.Text = string.Empty;
        }
    }

    public async void onButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(SeriesEntry.Text))
            {
                MyWorkouts.Add(SeriesEntry.Text.Trim());
                SeriesEntry.Text = string.Empty;
            }

            if (MyWorkouts.Count == 0)
            {
                await DisplayAlert("Attention", "Aucune série à enregistrer", "OK");
                return;
            }

            var data = await _workoutService.LoadWorkoutsAsync();

            var existing = data.Workouts
                .FirstOrDefault(w => w.Date.Date == _selectedDate.Date);

            if (existing != null)
            {
                existing.Exercises = MyWorkouts.Select(name => new Exercise
                {
                    Name = name,
                    PlannedSets = new List<WorkoutSet>(),
                    ActualSets = new List<WorkoutSet>()
                }).ToList();
            }
            else
            {
                var newWorkout = new Workout
                {
                    Id = data.Workouts.Count + 1,
                    Date = _selectedDate,
                    Status = "planned",
                    DurationSeconds = 0,
                    Exercises = MyWorkouts.Select(name => new Exercise
                    {
                        Name = name,
                        PlannedSets = new List<WorkoutSet>(),
                        ActualSets = new List<WorkoutSet>()
                    }).ToList()
                };
                data.Workouts.Add(newWorkout);
            }

            await _workoutService.SaveWorkoutsAsync(data);
            await DisplayAlert("Succès",
                $"Séance planifiée pour le {_selectedDate:dd/MM/yyyy}", "OK");

            MyWorkouts.Clear();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Erreur : {ex.Message}", "OK");
        }
    }
}