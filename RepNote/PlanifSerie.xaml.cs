using RepNote.Models;
using RepNote.Services;
using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 29.04.2026*/
namespace RepNote;

public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> MyWorkouts { get; set; } = [];
    public PlanifSerie()
	{
		InitializeComponent();
        SeriesList.ItemsSource = MyWorkouts;
	}

    public void OnAddSeriesClicked(object sender, EventArgs e)
    {
        //recupération de l'input
        string workoutName = SeriesEntry.Text;

        if (!string.IsNullOrEmpty(workoutName)){

            MyWorkouts.Add(workoutName);

            SeriesEntry.Text = string.Empty;
        }

    }

    public async void onButtonClicked(object sender, EventArgs e)
    {
        var service = new WorkoutService();

        var data = await service.LoadWorkoutsAsync();

        var newWorkout = new Workout
        {
            Id = data.Workouts.Count + 1,
            Date = DateTime.Now,
            Status = "completed",
            DurationSeconds = 0
        };



        await Shell.Current.GoToAsync("..");
    }

}