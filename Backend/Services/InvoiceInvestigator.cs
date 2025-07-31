namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing invoices.
    /// </summary>
    public class InvoiceInvestigator : Investigator
    {
        /// <summary>
        /// Begins invoice investigation operations.
        /// </summary>
        public override void Start()
        {
            Log(" Invoice Investigator started.");
            // Add logic here to scan invoices
        }

        /// <summary>
        /// Stops invoice investigation operations.
        /// </summary>
        public override void Stop()
        {
            Log(" Invoice Investigator stopped.");
        }
    }
}
