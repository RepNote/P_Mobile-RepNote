using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 29.04.2026*/
namespace RepNote;

public partial class PlanifSerie : ContentPage
{
    public ObservableCollection<string> AddedSeries { get; set; }

    public PlanifSerie()
    {
        InitializeComponent();

        AddedSeries = new ObservableCollection<string>();

        SeriesList.ItemsSource = AddedSeries;
    }

    private void OnAddSeriesClicked(object sender, EventArgs e)
    {
        string seriesName = SeriesEntry.Text;

        if (!string.IsNullOrWhiteSpace(seriesName))
        {
            AddedSeries.Add(seriesName);

            SeriesEntry.Text = string.Empty;
            SeriesEntry.Unfocus();
        }
    }
}