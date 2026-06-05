using RepNote.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RepNote;

/*  Lieu: ETML
    Auteur: Ryan Läuppi (Ryancmoi)
    Date: 13.05.2026*/
[QueryProperty(nameof(SelectedDateString), "date")]
public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> MyWorkouts { get; set; } = new();
    private readonly WorkoutService _workoutService;
    private DateTime _selectedDate = DateTime.Now;

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

    /// <summary>
    /// Charge les exercices planifiés existants à l'affichage
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadExistingWorkoutsAsync();
    }

    /// <summary>
    /// Charge uniquement les exercices planifiés depuis le JSON
    /// </summary>
    private async Task LoadExistingWorkoutsAsync()
    {
        var data = await _workoutService.LoadWorkoutsAsync();

        var existing = data.Workouts
            .FirstOrDefault(w => w.Date.Date == _selectedDate.Date);

        if (existing != null)
        {
            MyWorkouts.Clear();
            // Where(e => e.IsPlanned) : charge uniquement les exercices planifiés, pas ceux ajoutés en séance
            foreach (var exercise in existing.Exercises.Where(e => e.IsPlanned))
            {
                MyWorkouts.Add(exercise.Name);
            }
        }
    }

    /// <summary>
    /// Initialise la liste et le service
    /// </summary>
    public PlanifSerie()
    {
        InitializeComponent();
        _workoutService = new WorkoutService();
        SeriesList.ItemsSource = MyWorkouts;
    }

    /// <summary>
    /// Sauvegarde l'état actuel des exercices planifiés sans toucher aux exercices de séance
    /// </summary>
    private async Task SaveWorkoutsStateAsync()
    {
        var data = await _workoutService.LoadWorkoutsAsync();

        var existing = data.Workouts
            .FirstOrDefault(w => w.Date.Date == _selectedDate.Date);

        if (existing != null)
        {
            // Garde les exercices ajoutés en séance pour ne pas les écraser lors de la sauvegarde
            var sessionExercises = existing.Exercises.Where(e => !e.IsPlanned).ToList();

            if (MyWorkouts.Count == 0)
            {
                // Si plus aucun exercice planifié ET aucun exercice de séance : supprime le workout entier
                if (sessionExercises.Count == 0)
                    data.Workouts.Remove(existing);
                else
                    existing.Exercises = sessionExercises;
            }
            else
            {
                var updatedExercises = new List<Exercise>();
                foreach (var name in MyWorkouts)
                {
                    // Réutilise l'exercice existant s'il existe pour ne pas perdre les séries déjà planifiées
                    var oldExercise = existing.Exercises.FirstOrDefault(e => e.Name == name && e.IsPlanned);
                    updatedExercises.Add(oldExercise ?? new Exercise
                    {
                        Name = name,
                        IsPlanned = true,
                        PlannedSets = new List<WorkoutSet>(),
                        ActualSets = new List<WorkoutSet>()
                    });
                }
                // Rajoute les exercices de séance à la fin de la liste mise à jour
                updatedExercises.AddRange(sessionExercises);
                existing.Exercises = updatedExercises;
            }
        }
        // Aucun workout existant pour cette date : on en crée un nouveau avec tous les exercices planifiés
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
                    IsPlanned = true,
                    PlannedSets = new List<WorkoutSet>(),
                    ActualSets = new List<WorkoutSet>()
                }).ToList()
            };
            data.Workouts.Add(newWorkout);
        }

        await _workoutService.SaveWorkoutsAsync(data);
    }

    /// <summary>
    /// Ajoute un exercice à la liste et sauvegarde
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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

    /// <summary>
    /// Navigue vers la page de planification des séries d'un exercice
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void OnEditExerciseClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var exerciseName = button?.CommandParameter as string;
        if (!string.IsNullOrEmpty(exerciseName))
        {
            await Shell.Current.GoToAsync($"PlanifExercice?exerciseName={Uri.EscapeDataString(exerciseName)}&date={_selectedDate:yyyy-MM-dd}");
        }
    }

    /// <summary>
    /// Supprime un exercice de la planification et sauvegarde
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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

    /// <summary>
    /// Valide et sauvegarde la planification puis retourne à l'accueil
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void onButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Ajoute le texte de la saisie s'il n'a pas encore été validé avec le bouton "Ajouter"
            if (!string.IsNullOrWhiteSpace(SeriesEntry.Text))
            {
                MyWorkouts.Add(SeriesEntry.Text.Trim());
                SeriesEntry.Text = string.Empty;
            }

            if (MyWorkouts.Count == 0)
            {
                await DisplayAlert("Attention", "Aucun exercice à enregistrer", "OK");
                return;
            }

            await SaveWorkoutsStateAsync();

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
