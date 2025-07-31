namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing invoices.
    /// </summary>
    public class InvoiceInvestigator : Investigator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceInvestigator"/> class.
        /// </summary>
        public InvoiceInvestigator() : base("Invoice Investigator")
        {
        }

        /// <summary>
        /// Begins invoice investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            // Add logic here to scan invoices
        }

        /// <summary>
        /// Stops invoice investigation operations.
        /// </summary>
        protected override void OnStop()
        {
        }
    }
}
