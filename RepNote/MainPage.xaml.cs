using Plugin.Maui.Calendar.Models;
namespace RepNote
{
    public partial class MainPage : ContentPage
    {
        public EventCollection Events { get; set; }
        public DateTime Today { get; set; } = DateTime.Now;

        public MainPage()
        {
            InitializeComponent();
            Events = new EventCollection
            {
                [DateTime.Now] = new List<EventModel>
                {
                    new EventModel { Name = "Développé couché", Description = "4 séries de 12 reps" },
                    new EventModel { Name = "Squat", Description = "3 séries de 10 reps" }
                },
                [DateTime.Now.AddDays(2)] = new List<EventModel>
                {
                    new EventModel { Name = "Tirage dos", Description = "Lourd - 8 reps" }
                }
            };

            BindingContext = this;
        }

        public async void onClickButton1(object sender, EventArgs e) 
        {
            await Shell.Current.GoToAsync("SeanceEnCours");
        }

        public async void onClickButton2(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("PlanifEnCours");
        }
    }

    public class EventModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
