using System.Collections.ObjectModel;
/*  Lieu: ETML
    Auteur: Thomas Peltier
    Date: 29.04.2026*/
namespace RepNote;

public partial class PlanifSerie : ContentPage
{
	public PlanifSerie()
	{
		InitializeComponent();
	}
    public async void onButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}