namespace RepNote
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("PlanifSerie", typeof(PlanifSerie));
            Routing.RegisterRoute("SeanceEnCours", typeof(SeanceEnCours));
            Routing.RegisterRoute("PlanifExercice", typeof(PlanifExercice));
            Routing.RegisterRoute("AjoutSeriesSeance", typeof(AjoutSeriesSeance));
        }
    }
}
