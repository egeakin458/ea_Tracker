namespace ea_Tracker.Services
{
    public class WaybillInvestigator : Investigator
    {
        public override void Start()
        {
            Log(" Waybill Investigator started.");
            // Add logic here to scan waybills
        }

        public override void Stop()
        {
            Log(" Waybill Investigator stopped.");
        }
    }
}

