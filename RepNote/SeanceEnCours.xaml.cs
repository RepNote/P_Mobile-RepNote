using System.Collections.ObjectModel;
using RepNote.Models;
using RepNote.Services;

namespace RepNote;

public partial class SeanceEnCours : ContentPage
{
    private IDispatcherTimer _timer;
    private DateTime _startTime;
    private TimeSpan _elapsed;

    private readonly WorkoutService _workoutService = new();
    private readonly DateTime _sessionDate = DateTime.Now.Date;

    public ObservableCollection<ExerciseDisplay> PlannedExercises { get; } = new();
    public ObservableCollection<ExerciseDisplay> AddedExercises { get; } = new();

    public SeanceEnCours()
    {
        InitializeComponent();
        PlannedExercisesList.ItemsSource = PlannedExercises;
        AddedExercisesList.ItemsSource = AddedExercises;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartTimer();
        await LoadExercisesAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimer();
    }

    // ---------- Chronomètre ----------
    private void StartTimer()
    {
        _startTime = DateTime.Now;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        _elapsed = DateTime.Now - _startTime;
        string format = _elapsed.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss";
        TimerLabel.Text = $"Temps écoulé : {_elapsed.ToString(format)}";
    }

    // ---------- Chargement des exercices ----------
    private async Task LoadExercisesAsync()
    {
        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        PlannedExercises.Clear();
        AddedExercises.Clear();

        if (workout?.Exercises != null)
        {
            foreach (var ex in workout.Exercises)
            {
                // Planifié = a au moins une série prévue
                if (ex.PlannedSets is { Count: > 0 })
                    PlannedExercises.Add(ToDisplay(ex));
                else
                    AddedExercises.Add(ToDisplay(ex));
            }
        }

        EmptyPlannedLabel.IsVisible = PlannedExercises.Count == 0;
        EmptyAddedLabel.IsVisible = AddedExercises.Count == 0;
    }

    private ExerciseDisplay ToDisplay(Exercise ex)
    {
        string summary;

        if (ex.PlannedSets is { Count: > 0 })
        {
            // Exercice planifié → affiche les séries prévues
            summary = string.Join("  •  ",
                ex.PlannedSets.Select((s, i) => $"S{i + 1} : {s.Reps} reps × {s.Weight} kg"));
        }
        else if (ex.ActualSets is { Count: > 0 })
        {
            // Exercice ajouté avec des séries réalisées
            summary = string.Join("  •  ",
                ex.ActualSets.Select((s, i) => $"S{i + 1} : {s.Reps} reps × {s.Weight} kg"));
        }
        else
        {
            summary = "Aucune série ajoutée";
        }

        return new ExerciseDisplay
        {
            Name = ex.Name,
            SetsSummary = summary
        };
    }

    // ---------- Ajout d'un nouvel exercice ----------
    private async void OnAjouterExerciceTapped(object sender, EventArgs e)
    {
        var name = NomExerciceEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        bool alreadyExists =
            PlannedExercises.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
            AddedExercises.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            await DisplayAlert("Doublon", "Cet exercice est déjà dans la séance.", "OK");
            return;
        }

        // Sauvegarde dans le JSON
        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        if (workout == null)
        {
            workout = new Workout
            {
                Id = root.Workouts.Count + 1,
                Date = _sessionDate,
                Status = "in_progress",
                DurationSeconds = (int)_elapsed.TotalSeconds,
                Exercises = new List<Exercise>()
            };
            root.Workouts.Add(workout);
        }

        var newExercise = new Exercise
        {
            Name = name,
            PlannedSets = new List<WorkoutSet>(),
            ActualSets = new List<WorkoutSet>()
        };
        workout.Exercises.Add(newExercise);
        await _workoutService.SaveWorkoutsAsync(root);

        // Va dans la liste des exercices ajoutés (pas planifiés)
        AddedExercises.Add(ToDisplay(newExercise));
        NomExerciceEntry.Text = string.Empty;
        EmptyAddedLabel.IsVisible = false;
    }

    // ---------- Fin de séance ----------
    public async void onButtonClicked(object sender, EventArgs e)
    {
        StopTimer();

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        if (workout != null)
        {
            workout.Status = "completed";
            workout.DurationSeconds = (int)_elapsed.TotalSeconds;
            await _workoutService.SaveWorkoutsAsync(root);
        }

        await Shell.Current.GoToAsync("..");
    }

    // ---------- DTO pour l'affichage ----------
    public class ExerciseDisplay
    {
        public string Name { get; set; }
        public string SetsSummary { get; set; }
    }
}