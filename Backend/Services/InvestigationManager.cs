namespace ea_Tracker.Services
{
    /// <summary>
    /// Manages a collection of investigators and coordinates their lifecycle.
    /// </summary>
    public class InvestigationManager
    {
        private readonly List<Investigator> _investigators;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationManager"/> class.
        /// </summary>
        /// <param name="investigators">The investigators to manage.</param>
        public InvestigationManager(List<Investigator> investigators)
        {
            _investigators = investigators;
        }

        /// <summary>
        /// Starts all registered investigators.
        /// </summary>
        public void StartAll()
        {
            foreach (var inv in _investigators)
                inv.Start();
        }

        /// <summary>
        /// Stops all registered investigators.
        /// </summary>
        public void StopAll()
        {
            foreach (var inv in _investigators)
                inv.Stop();
        }
    }
}

