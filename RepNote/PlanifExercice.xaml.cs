using System.Collections.ObjectModel;
using RepNote.Models;
using RepNote.Services;

namespace RepNote;

/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 06.05.2026*/
public partial class PlanifExercice : ContentPage
{
    public ObservableCollection<SerieData> AddedSeries { get; set; }
    private WorkoutService _workoutService = new WorkoutService();

    public PlanifExercice()
    {
        InitializeComponent();
        AddedSeries = new ObservableCollection<SerieData>();
        SeriesList.ItemsSource = AddedSeries;

        _ = LoadSavedData();
    }

    private async Task LoadSavedData()
    {
        var root = await _workoutService.LoadWorkoutsAsync();

        if (root.Workouts != null && root.Workouts.Any())
        {
            var lastWorkout = root.Workouts.Last();
            var lastExercise = lastWorkout.Exercises.LastOrDefault();

            if (lastExercise != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ExerciseNameLabel.Text = lastExercise.Name;
                    AddedSeries.Clear();
                    int count = 1;
                    foreach (var set in lastExercise.PlannedSets)
                    {
                        AddedSeries.Add(new SerieData
                        {
                            Numero = $"Série {count++}",
                            Reps = set.Reps.ToString(),
                            Poids = set.Weight.ToString()
                        });
                    }
                });
            }
        }
    }

    private async Task SaveCurrentState()
    {
        var root = await _workoutService.LoadWorkoutsAsync();

        var currentWorkout = new Workout
        {
            Id = 1,
            Date = DateTime.Now,
            Status = "In Progress",
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Name = ExerciseNameLabel.Text,
                    PlannedSets = AddedSeries.Select(s => new WorkoutSet
                    {
                        Reps = int.TryParse(s.Reps, out int r) ? r : 0,
                        Weight = double.TryParse(s.Poids, out double p) ? p : 0
                    }).ToList()
                }
            }
        };

        root.Workouts = new List<Workout> { currentWorkout };
        await _workoutService.SaveWorkoutsAsync(root);
    }

    private void OnModifyExerciseClicked(object sender, EventArgs e)
    {
        ExerciseNameLabel.IsVisible = false;
        ExerciseEditEntry.IsVisible = true;
        ExerciseEditEntry.Focus();
        ModifyBtn.IsVisible = false;
    }

    private async void OnExerciseNameCompleted(object sender, EventArgs e)
    {
        ExerciseNameLabel.Text = ExerciseEditEntry.Text;
        ExerciseNameLabel.IsVisible = true;
        ExerciseEditEntry.IsVisible = false;
        ModifyBtn.IsVisible = true;
        await SaveCurrentState();
    }

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

    public class SerieData : System.ComponentModel.INotifyPropertyChanged
    {
        private string _numero;
        public string Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); }
        }
        public string Reps { get; set; }
        public string Poids { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}