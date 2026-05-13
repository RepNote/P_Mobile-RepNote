using RepNote.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Ryan Läuppi
    Date: 13.05.2026*/
namespace RepNote;
public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> MyWorkouts { get; set; } = [];
    private readonly WorkoutService _workoutService;

    public PlanifSerie()
    {
        InitializeComponent();
        _workoutService = new WorkoutService();
        SeriesList.ItemsSource = MyWorkouts;
    }

    public async void OnAddSeriesClicked(object sender, EventArgs e)
    {
        //recupération de l'input
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
            // Charger les données existantes
            var data = await _workoutService.LoadWorkoutsAsync();

            // Créer un nouveau workout
            var newWorkout = new Workout
            {
                Id = data.Workouts.Count + 1,
                Date = DateTime.Now,
                Status = "completed",
                DurationSeconds = 0
            };

            // Ajouter le workout à la liste
            data.Workouts.Add(newWorkout);

            // Sauvegarder dans le JSON
            await _workoutService.SaveWorkoutsAsync(data);

            // Afficher un message de succès
            await DisplayAlert("Succès", "Séance terminée et sauvegardée", "OK");

            // Réinitialiser les séries
            MyWorkouts.Clear();

            // Revenir à la page précédente
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde : {ex.Message}", "OK");
        }
    }
}