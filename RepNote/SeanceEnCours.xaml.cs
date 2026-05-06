namespace RepNote;

public partial class SeanceEnCours : ContentPage
{
	public SeanceEnCours()
	{
        InitializeComponent();
	}

	public async void onButtonClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}