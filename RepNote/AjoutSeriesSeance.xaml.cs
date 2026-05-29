using System.Collections.ObjectModel;
using System.Globalization;
using RepNote.Models;
using RepNote.Services;

namespace RepNote;

[QueryProperty(nameof(ExerciseName), "exerciseName")]
[QueryProperty(nameof(SelectedDateString), "date")]
public partial class AjoutSeriesSeance : ContentPage
{
    private readonly WorkoutService _workoutService = new();

    public ObservableCollection<SerieItem> Series { get; } = new();

    private string _exerciseName;
    public string ExerciseName
    {
        set
        {
            _exerciseName = Uri.UnescapeDataString(value ?? string.Empty);
            if (ExerciseNameLabel != null)
                ExerciseNameLabel.Text = _exerciseName;
        }
    }

    private DateTime _sessionDate = DateTime.Now.Date;
    public string SelectedDateString
    {
        set
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                _sessionDate = d.Date;
            }
        }
    }

    /// <summary>
    /// Initialise la liste des séries
    /// </summary>
    public AjoutSeriesSeance()
    {
        InitializeComponent();
        SeriesList.ItemsSource = Series;
    }

    /// <summary>
    /// Affiche le nom de l'exercice et charge les séries existantes
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ExerciseNameLabel.Text = _exerciseName;
        await LoadSeriesAsync();
    }

    /// <summary>
    /// Charge les séries réalisées depuis le JSON
    /// </summary>
    private async Task LoadSeriesAsync()
    {
        if (string.IsNullOrEmpty(_exerciseName)) return;

        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);
        var exercise = workout?.Exercises.FirstOrDefault(e => e.Name == _exerciseName && !e.IsPlanned);

        Series.Clear();
        if (exercise?.ActualSets != null)
        {
            int i = 1;
            foreach (var set in exercise.ActualSets)
            {
                Series.Add(new SerieItem
                {
                    Numero = $"Série {i++}",
                    Reps = set.Reps,
                    Poids = set.Weight
                });
            }
        }

        EmptyLabel.IsVisible = Series.Count == 0;
    }

    /// <summary>
    /// Sauvegarde les séries dans le JSON
    /// </summary>
    private async Task SaveSeriesAsync()
    {
        var root = await _workoutService.LoadWorkoutsAsync();
        var workout = root.Workouts.FirstOrDefault(w => w.Date.Date == _sessionDate);
        if (workout == null)
        {
            System.Diagnostics.Debug.WriteLine($"[AjoutSeries] Workout introuvable pour la date {_sessionDate:yyyy-MM-dd}");
            return;
        }

        var exercise = workout.Exercises.FirstOrDefault(e => e.Name == _exerciseName && !e.IsPlanned);
        if (exercise == null)
        {
            System.Diagnostics.Debug.WriteLine($"[AjoutSeries] Exercice '{_exerciseName}' introuvable dans le workout");
            return;
        }

        exercise.ActualSets = Series.Select(s => new WorkoutSet
        {
            Reps = s.Reps,
            Weight = s.Poids
        }).ToList();

        await _workoutService.SaveWorkoutsAsync(root);
    }

    /// <summary>
    /// Renumérote les séries après une suppression
    /// </summary>
    private void RenumeroterSeries()
    {
        for (int i = 0; i < Series.Count; i++)
            Series[i].Numero = $"Série {i + 1}";
    }

    /// <summary>
    /// Ajoute une nouvelle série après validation des champs
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAddSerieTapped(object sender, EventArgs e)
    {
        if (!int.TryParse(RepsEntry.Text, out int reps) || reps <= 0)
        {
            await DisplayAlert("Erreur", "Entre un nombre de répétitions valide.", "OK");
            return;
        }
        if (!double.TryParse(PoidsEntry.Text, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double poids) || poids < 0)
        {
            if (!double.TryParse(PoidsEntry.Text?.Replace(',', '.'),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out poids) || poids < 0)
            {
                await DisplayAlert("Erreur", "Entre un poids valide.", "OK");
                return;
            }
        }

        Series.Add(new SerieItem
        {
            Numero = $"Série {Series.Count + 1}",
            Reps = reps,
            Poids = poids
        });

        RepsEntry.Text = string.Empty;
        PoidsEntry.Text = string.Empty;
        EmptyLabel.IsVisible = false;

        await SaveSeriesAsync();
    }

    /// <summary>
    /// Modifie les répétitions et le poids d'une série existante
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnEditSerieClicked(object sender, EventArgs e)
    {
        var serie = (sender as Button)?.CommandParameter as SerieItem;
        if (serie == null) return;

        var newReps = await DisplayPromptAsync("Modifier", "Répétitions",
            initialValue: serie.Reps.ToString(), keyboard: Keyboard.Numeric);
        if (newReps == null) return;

        var newPoids = await DisplayPromptAsync("Modifier", "Poids (kg)",
            initialValue: serie.Poids.ToString(CultureInfo.InvariantCulture),
            keyboard: Keyboard.Numeric);
        if (newPoids == null) return;

        if (int.TryParse(newReps, out int r) && r > 0)
            serie.Reps = r;

        if (double.TryParse(newPoids.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double p) && p >= 0)
            serie.Poids = p;

        await SaveSeriesAsync();
    }

    /// <summary>
    /// Supprime une série et renumérote la liste
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnDeleteSerieClicked(object sender, EventArgs e)
    {
        var serie = (sender as Button)?.CommandParameter as SerieItem;
        if (serie == null) return;

        Series.Remove(serie);
        RenumeroterSeries();
        EmptyLabel.IsVisible = Series.Count == 0;

        await SaveSeriesAsync();
    }

    /// <summary>
    /// Retourne à la séance en cours
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnRetourClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// DTO pour l'affichage d'une série dans la liste
    /// </summary>
    public class SerieItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _numero;
        private int _reps;
        private double _poids;

        public string Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); OnPropertyChanged(nameof(Details)); }
        }
        public int Reps
        {
            get => _reps;
            set { _reps = value; OnPropertyChanged(); OnPropertyChanged(nameof(Details)); }
        }
        public double Poids
        {
            get => _poids;
            set { _poids = value; OnPropertyChanged(); OnPropertyChanged(nameof(Details)); }
        }

        public string Details => $"{Reps} reps × {Poids} kg";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
