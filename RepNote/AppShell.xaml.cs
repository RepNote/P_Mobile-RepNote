namespace RepNote
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("PlanifSerie", typeof(PlanifSerie));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("SeanceEnCours", typeof(SeanceEnCours));
            Routing.RegisterRoute("PlanifExercice", typeof(PlanifExercice));
        }
    }
}
