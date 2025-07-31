namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing waybills.
    /// </summary>
    public class WaybillInvestigator : Investigator
    {
        /// <summary>
        /// Begins waybill investigation operations.
        /// </summary>
        public override void Start()
        {
            Log(" Waybill Investigator started.");
            // Add logic here to scan waybills
        }

        /// <summary>
        /// Stops waybill investigation operations.
        /// </summary>
        public override void Stop()
        {
            Log(" Waybill Investigator stopped.");
        }
    }
}

