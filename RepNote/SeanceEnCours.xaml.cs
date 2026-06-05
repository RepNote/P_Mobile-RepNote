using System.Collections.ObjectModel;
using RepNote.Models;
using RepNote.Services;

namespace RepNote;

public partial class SeanceEnCours : ContentPage
{
    private IDispatcherTimer _timer;
    private DateTime _startTime;
    private TimeSpan _elapsed;
    private TimeSpan _accumulatedElapsed;

    private readonly WorkoutService _workoutService = new();
    private readonly DateTime _sessionDate = DateTime.Now.Date;

    public ObservableCollection<ExerciseDisplay> PlannedExercises { get; } = new();
    public ObservableCollection<ExerciseDisplay> AddedExercises { get; } = new();

    /// <summary>
    /// Initialise les listes d'exercices
    /// </summary>
    public SeanceEnCours()
    {
        InitializeComponent();
        PlannedExercisesList.ItemsSource = PlannedExercises;
        AddedExercisesList.ItemsSource = AddedExercises;
    }

    /// <summary>
    /// Démarre le chronomètre à l'affichage de la page
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartTimer();
    }

    /// <summary>
    /// Recharge les exercices après chaque navigation
    /// </summary>
    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await LoadExercisesAsync();
    }

    /// <summary>
    /// Arrête le chronomètre quand on quitte la page
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimer();
    }

    /// <summary>
    /// Crée et démarre le timer d'une seconde
    /// </summary>
    private void StartTimer()
    {
        _startTime = DateTime.Now;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <summary>
    /// Accumule le temps écoulé et arrête le timer
    /// </summary>
    private void StopTimer()
    {
        if (_timer != null)
        {
            // Sauvegarde le temps écoulé avant d'arrêter pour pouvoir reprendre sans perdre le cumul
            _accumulatedElapsed += DateTime.Now - _startTime;
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    /// <summary>
    /// Met à jour l'affichage du chronomètre chaque seconde
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTimerTick(object sender, EventArgs e)
    {
        _elapsed = _accumulatedElapsed + (DateTime.Now - _startTime);
        // Passe au format hh:mm:ss si la séance dépasse 1 heure
        string format = _elapsed.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss";
        TimerLabel.Text = $"Temps écoulé : {_elapsed.ToString(format)}";
    }

    /// <summary>
    /// Charge les exercices planifiés et ajoutés depuis le JSON
    /// </summary>
    private async Task LoadExercisesAsync()
    {
        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        System.Diagnostics.Debug.WriteLine($"[SeanceEnCours] Workout trouvé: {workout != null}, date: {_sessionDate:yyyy-MM-dd}");

        PlannedExercises.Clear();
        AddedExercises.Clear();

        if (workout?.Exercises != null)
        {
            // Dispatch chaque exercice dans la bonne liste selon s'il est planifié ou ajouté en séance
            foreach (var ex in workout.Exercises)
            {
                System.Diagnostics.Debug.WriteLine($"[SeanceEnCours] Exercice: {ex.Name}, IsPlanned: {ex.IsPlanned}, ActualSets: {ex.ActualSets?.Count ?? 0}");
                if (ex.IsPlanned)
                    PlannedExercises.Add(ToDisplay(ex));
                else
                    AddedExercises.Add(ToDisplay(ex));
            }
        }

        EmptyPlannedLabel.IsVisible = PlannedExercises.Count == 0;
        EmptyAddedLabel.IsVisible = AddedExercises.Count == 0;
    }

    /// <summary>
    /// Convertit un exercice en objet d'affichage avec résumé des séries
    /// </summary>
    /// <param name="ex"></param>
    private ExerciseDisplay ToDisplay(Exercise ex)
    {
        string summary;

        // Priorité d'affichage : séries planifiées > séries réalisées > message vide
        if (ex.PlannedSets is { Count: > 0 })
        {
            summary = string.Join("  •  ",
                ex.PlannedSets.Select((s, i) => $"S{i + 1} : {s.Reps} reps × {s.Weight} kg"));
        }
        else if (ex.ActualSets is { Count: > 0 })
        {
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

    /// <summary>
    /// Ajoute un nouvel exercice à la séance
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAjouterExerciceTapped(object sender, EventArgs e)
    {
        var name = NomExerciceEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        // OrdinalIgnoreCase : la comparaison ignore la casse (ex: "Squat" == "squat")
        bool alreadyExists =
            AddedExercises.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            await DisplayAlert("Doublon", "Cet exercice est déjà dans la séance.", "OK");
            return;
        }

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        // Crée un nouveau workout pour aujourd'hui si aucun n'existe encore
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
            IsPlanned = false,
            PlannedSets = new List<WorkoutSet>(),
            ActualSets = new List<WorkoutSet>()
        };
        workout.Exercises.Add(newExercise);
        await _workoutService.SaveWorkoutsAsync(root);

        AddedExercises.Add(ToDisplay(newExercise));
        NomExerciceEntry.Text = string.Empty;
        EmptyAddedLabel.IsVisible = false;
    }

    /// <summary>
    /// Navigue vers la page d'ajout de séries pour l'exercice sélectionné
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAddedExerciseTapped(object sender, TappedEventArgs e)
    {
        var name = e.Parameter as string;
        if (string.IsNullOrEmpty(name)) return;

        await Shell.Current.GoToAsync(
            $"AjoutSeriesSeance?exerciseName={Uri.EscapeDataString(name)}&date={_sessionDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// Renomme un exercice ajouté pendant la séance
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnEditAddedExerciseClicked(object sender, EventArgs e)
    {
        var name = (sender as Button)?.CommandParameter as string;
        if (string.IsNullOrEmpty(name)) return;

        var newName = await DisplayPromptAsync("Modifier", "Nouveau nom de l'exercice", initialValue: name);
        newName = newName?.Trim();
        if (string.IsNullOrEmpty(newName) || newName == name) return;

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);
        // !ex.IsPlanned : on ne modifie que les exercices ajoutés en séance, pas les planifiés
        var exercise = workout?.Exercises.FirstOrDefault(ex => ex.Name == name && !ex.IsPlanned);
        if (exercise == null) return;

        exercise.Name = newName;
        await _workoutService.SaveWorkoutsAsync(root);
        await LoadExercisesAsync();
    }

    /// <summary>
    /// Supprime un exercice ajouté pendant la séance
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnDeleteAddedExerciseClicked(object sender, EventArgs e)
    {
        var name = (sender as Button)?.CommandParameter as string;
        if (string.IsNullOrEmpty(name)) return;

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);
        var exercise = workout?.Exercises.FirstOrDefault(ex => ex.Name == name && !ex.IsPlanned);
        if (exercise == null) return;

        workout.Exercises.Remove(exercise);
        await _workoutService.SaveWorkoutsAsync(root);
        await LoadExercisesAsync();
    }

    /// <summary>
    /// Termine la séance, sauvegarde la durée et revient à l'accueil
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void onButtonClicked(object sender, EventArgs e)
    {
        StopTimer();

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);

        if (workout != null)
        {
            workout.Status = "completed";
            // += et non = pour cumuler avec la durée déjà enregistrée si la séance a été interrompue
            workout.DurationSeconds += (int)_elapsed.TotalSeconds;
            await _workoutService.SaveWorkoutsAsync(root);
        }

        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// DTO pour l'affichage d'un exercice dans la liste
    /// </summary>
    public class ExerciseDisplay
    {
        public string Name { get; set; }
        public string SetsSummary { get; set; }
    }
}
