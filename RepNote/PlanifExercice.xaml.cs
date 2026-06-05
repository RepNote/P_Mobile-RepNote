using System.Collections.ObjectModel;
using System.Globalization;
using RepNote.Models;
using RepNote.Services;

namespace RepNote;

/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 06.05.2026*/
[QueryProperty(nameof(ExerciseName), "exerciseName")]
[QueryProperty(nameof(SelectedDateString), "date")]
public partial class PlanifExercice : ContentPage
{
    public ObservableCollection<SerieData> AddedSeries { get; set; }
    private WorkoutService _workoutService = new WorkoutService();

    private string _exerciseName;
    public string ExerciseName
    {
        get => _exerciseName;
        set
        {
            _exerciseName = Uri.UnescapeDataString(value);
            OnPropertyChanged();
        }
    }

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
    /// Initialise la liste des séries planifiées
    /// </summary>
    public PlanifExercice()
    {
        InitializeComponent();
        AddedSeries = new ObservableCollection<SerieData>();
        SeriesList.ItemsSource = AddedSeries;
    }

    /// <summary>
    /// Charge les données sauvegardées à l'affichage de la page
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSavedData();
    }

    /// <summary>
    /// Charge les séries planifiées de l'exercice depuis le JSON
    /// </summary>
    private async Task LoadSavedData()
    {
        if (string.IsNullOrEmpty(_exerciseName))
            return;

        var root = await _workoutService.LoadWorkoutsAsync();

        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _selectedDate.Date);
        if (workout != null)
        {
            var exercise = workout.Exercises.FirstOrDefault(e => e.Name == _exerciseName);
            if (exercise != null)
            {
                // BeginInvokeOnMainThread : les modifications UI doivent s'exécuter sur le thread principal
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ExerciseNameLabel.Text = exercise.Name;
                    AddedSeries.Clear();
                    int count = 1;
                    foreach (var set in exercise.PlannedSets)
                    {
                        AddedSeries.Add(new SerieData
                        {
                            Numero = $"Série {count++}",
                            Reps = set.Reps.ToString(),
                            Poids = set.Weight.ToString()
                        });
                    }
                });
                return;
            }
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ExerciseNameLabel.Text = _exerciseName;
            AddedSeries.Clear();
        });
    }

    /// <summary>
    /// Sauvegarde les séries planifiées de l'exercice dans le JSON
    /// </summary>
    private async Task SaveCurrentState()
    {
        if (string.IsNullOrEmpty(_exerciseName))
            return;

        var root = await _workoutService.LoadWorkoutsAsync();

        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _selectedDate.Date);
        // Crée un workout vide pour la date sélectionnée s'il n'existe pas encore
        if (workout == null)
        {
            workout = new Workout
            {
                Id = root.Workouts.Count + 1,
                Date = _selectedDate,
                Status = "planned",
                DurationSeconds = 0,
                Exercises = new List<Exercise>()
            };
            root.Workouts.Add(workout);
        }

        var exercise = workout.Exercises.FirstOrDefault(e => e.Name == _exerciseName);
        if (exercise == null)
        {
            exercise = new Exercise
            {
                Name = _exerciseName,
                PlannedSets = new List<WorkoutSet>(),
                ActualSets = new List<WorkoutSet>()
            };
            workout.Exercises.Add(exercise);
        }

        // Convertit la liste d'affichage (SerieData) en modèle de données (WorkoutSet)
        exercise.PlannedSets = AddedSeries.Select(s => new WorkoutSet
        {
            Reps = int.TryParse(s.Reps, out int r) ? r : 0,
            Weight = double.TryParse(s.Poids, out double p) ? p : 0
        }).ToList();

        await _workoutService.SaveWorkoutsAsync(root);
    }

    /// <summary>
    /// Sauvegarde et retourne à la planification
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnTerminerClicked(object sender, EventArgs e)
    {
        await SaveCurrentState();
        await Shell.Current.GoToAsync("../..");
    }

    /// <summary>
    /// Affiche le champ de saisie pour modifier le nom de l'exercice
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnModifyExerciseClicked(object sender, EventArgs e)
    {
        ExerciseNameLabel.IsVisible = false;
        ExerciseEditEntry.IsVisible = true;
        ExerciseEditEntry.Focus();
        ModifyBtn.IsVisible = false;
    }

    /// <summary>
    /// Valide le nouveau nom de l'exercice et sauvegarde
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnExerciseNameCompleted(object sender, EventArgs e)
    {
        ExerciseNameLabel.Text = ExerciseEditEntry.Text;
        ExerciseNameLabel.IsVisible = true;
        ExerciseEditEntry.IsVisible = false;
        ModifyBtn.IsVisible = true;

        string oldName = _exerciseName;
        string newName = ExerciseEditEntry.Text;
        // Renomme l'exercice dans le JSON seulement si le nom a vraiment changé
        if (!string.IsNullOrEmpty(newName) && oldName != newName)
        {
            _exerciseName = newName;
            var root = await _workoutService.LoadWorkoutsAsync();
            var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _selectedDate.Date);
            if (workout != null)
            {
                var exercise = workout.Exercises.FirstOrDefault(e => e.Name == oldName);
                if (exercise != null)
                {
                    exercise.Name = newName;
                    await _workoutService.SaveWorkoutsAsync(root);
                }
            }
        }
        else
        {
            // Nom inchangé ou vide : sauvegarde simplement l'état sans renommer
            await SaveCurrentState();
        }
    }

    /// <summary>
    /// Ajoute une série planifiée et sauvegarde
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAddSerieClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RepsEntry.Text) || string.IsNullOrWhiteSpace(PoidsEntry.Text))
            return;

        AddedSeries.Add(new SerieData
        {
            Numero = $"Série {AddedSeries.Count + 1}",
            Reps = RepsEntry.Text,
            Poids = PoidsEntry.Text
        });

        RepsEntry.Text = string.Empty;
        PoidsEntry.Text = string.Empty;

        await SaveCurrentState();
    }

    /// <summary>
    /// Modifie les répétitions et le poids d'une série planifiée
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnEditSerieClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var serie = button?.CommandParameter as SerieData;
        if (serie == null) return;

        var newReps = await DisplayPromptAsync("Modifier la série", "Répétitions",
            initialValue: serie.Reps, keyboard: Keyboard.Numeric);
        if (newReps == null) return;

        var newPoids = await DisplayPromptAsync("Modifier la série", "Poids",
            initialValue: serie.Poids, keyboard: Keyboard.Numeric);
        if (newPoids == null) return;

        serie.Reps = newReps;
        serie.Poids = newPoids;
        await SaveCurrentState();
    }

    /// <summary>
    /// Supprime une série planifiée et renumérote la liste
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnDeleteSerieClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var serie = button?.CommandParameter as SerieData;

        if (serie != null)
        {
            AddedSeries.Remove(serie);
            for (int i = 0; i < AddedSeries.Count; i++)
            {
                AddedSeries[i].Numero = $"Série {i + 1}";
            }
            await SaveCurrentState();
        }
    }

    /// <summary>
    /// DTO pour l'affichage d'une série planifiée dans la liste
    /// </summary>
    public class SerieData : System.ComponentModel.INotifyPropertyChanged
    {
        private string _numero;
        private string _reps;
        private string _poids;

        public string Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); }
        }
        public string Reps
        {
            get => _reps;
            set { _reps = value; OnPropertyChanged(); }
        }
        public string Poids
        {
            get => _poids;
            set { _poids = value; OnPropertyChanged(); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
