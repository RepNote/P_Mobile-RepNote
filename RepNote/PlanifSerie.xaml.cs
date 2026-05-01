using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 29.04.2026*/
namespace RepNote;

public partial class PlanifSerie : ContentPage
{
    // La liste qui contient tes noms de séries
    public ObservableCollection<string> AddedSeries { get; set; }

    public PlanifSerie()
    {
        InitializeComponent();

        // Initialisation de la liste
        AddedSeries = new ObservableCollection<string>();

        // Liaison de la liste à la CollectionView
        SeriesList.ItemsSource = AddedSeries;
    }

    private void OnAddSeriesClicked(object sender, EventArgs e)
    {
        string seriesName = SeriesEntry.Text;

        if (!string.IsNullOrWhiteSpace(seriesName))
        {
            // Ajoute le nom à la liste (l'UI se met à jour toute seule)
            AddedSeries.Add(seriesName);

            // Vide le champ de saisie
            SeriesEntry.Text = string.Empty;
            SeriesEntry.Unfocus();
        }
    }
}