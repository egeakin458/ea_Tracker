namespace ea_Tracker.Services
{
    public class InvoiceInvestigator : Investigator
    {
        public override void Start()
        {
            Log(" Invoice Investigator started.");
            // Add logic here to scan invoices
        }

        public override void Stop()
        {
            Log(" Invoice Investigator stopped.");
        }
    }
}
