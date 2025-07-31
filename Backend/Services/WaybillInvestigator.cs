namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing waybills.
    /// </summary>
    public class WaybillInvestigator : Investigator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaybillInvestigator"/> class.
        /// </summary>
        public WaybillInvestigator() : base("Waybill Investigator")
        {
        }

        /// <summary>
        /// Begins waybill investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            // Add logic here to scan waybills
        }

        /// <summary>
        /// Stops waybill investigation operations.
        /// </summary>
        protected override void OnStop()
        {
        }
    }
}

