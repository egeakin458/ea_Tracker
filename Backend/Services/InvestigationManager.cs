namespace ea_Tracker.Services
{
    public class InvestigationManager
    {
        private readonly List<Investigator> _investigators;

        public InvestigationManager(List<Investigator> investigators)
        {
            _investigators = investigators;
        }

        public void StartAll()
        {
            foreach (var inv in _investigators)
                inv.Start();
        }

        public void StopAll()
        {
            foreach (var inv in _investigators)
                inv.Stop();
        }
    }
}

