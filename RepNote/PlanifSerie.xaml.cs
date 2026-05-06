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