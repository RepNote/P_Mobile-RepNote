using RepNote.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RepNote;

/*  Lieu: ETML
    Auteur: Ryan LÃ¤uppi (Ryancmoi)
    Date: 13.05.2026*/
[QueryProperty(nameof(SelectedDateString), "date")]
public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> MyWorkouts { get; set; } = new();
    private readonly WorkoutService _workoutService;
    private DateTime _selectedDate = DateTime.Now;

    // ReÃ§u depuis MainPage via Shell navigation
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

    private async Task SaveWorkoutsStateAsync()
    {
        var data = await _workoutService.LoadWorkoutsAsync();

        var existing = data.Workouts
            .FirstOrDefault(w => w.Date.Date == _selectedDate.Date);

        if (existing != null)
        {
            if (MyWorkouts.Count == 0)
            {
                data.Workouts.Remove(existing);
            }
            else
            {
                var updatedExercises = new List<Exercise>();
                foreach (var name in MyWorkouts)
                {
                    var oldExercise = existing.Exercises.FirstOrDefault(e => e.Name == name);
                    if (oldExercise != null)
                    {
                        updatedExercises.Add(oldExercise);
                    }
                    else
                    {
                        updatedExercises.Add(new Exercise
                        {
                            Name = name,
                            PlannedSets = new List<WorkoutSet>(),
                            ActualSets = new List<WorkoutSet>()
                        });
                    }
                }
                existing.Exercises = updatedExercises;
            }
        }
        else if (MyWorkouts.Count > 0)
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
    }

    public async void OnAddSeriesClicked(object sender, EventArgs e)
    {
        string workoutName = SeriesEntry.Text;
        if (!string.IsNullOrEmpty(workoutName))
        {
            MyWorkouts.Add(workoutName);
            SeriesEntry.Text = string.Empty;
            await SaveWorkoutsStateAsync();
        }
    }

    public async void OnEditExerciseClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var exerciseName = button?.CommandParameter as string;
        if (!string.IsNullOrEmpty(exerciseName))
        {
            await Shell.Current.GoToAsync($"PlanifExercice?exerciseName={Uri.EscapeDataString(exerciseName)}&date={_selectedDate:yyyy-MM-dd}");
        }
    }

    public async void OnDeleteExerciseClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var exerciseName = button?.CommandParameter as string;
        if (!string.IsNullOrEmpty(exerciseName))
        {
            MyWorkouts.Remove(exerciseName);
            await SaveWorkoutsStateAsync();
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
                await DisplayAlert("Attention", "Aucun exercice Ã  enregistrer", "OK");
                return;
            }

            await SaveWorkoutsStateAsync();

            await DisplayAlert("SuccÃ¨s",
                $"SÃ©ance planifiÃ©e pour le {_selectedDate:dd/MM/yyyy}", "OK");

            MyWorkouts.Clear();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Erreur : {ex.Message}", "OK");
        }
    }
}
