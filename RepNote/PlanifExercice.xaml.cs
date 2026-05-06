using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 29.04.2026*/
namespace RepNote;


public partial class PlanifExercice : ContentPage
{
    public ObservableCollection<SerieData> AddedSeries { get; set; }

    public PlanifExercice()
    {
        InitializeComponent();
        AddedSeries = new ObservableCollection<SerieData>();
        SeriesList.ItemsSource = AddedSeries;
    }


    private void OnAddSerieClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RepsEntry.Text) || string.IsNullOrWhiteSpace(PoidsEntry.Text))
            return;

        int nextNumber = AddedSeries.Count + 1;

        AddedSeries.Add(new SerieData
        {
            Numero = $"Série {nextNumber}",
            Reps = RepsEntry.Text,
            Poids = PoidsEntry.Text
        });

        // Nettoyage
        RepsEntry.Text = string.Empty;
        PoidsEntry.Text = string.Empty;
    }
    public class SerieData
    {
        public string Numero { get; set; } 
        public string Reps { get; set; }   
        public string Poids { get; set; }  
    }
}