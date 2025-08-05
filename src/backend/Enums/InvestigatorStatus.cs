namespace ea_Tracker.Enums
{
    /// <summary>
    /// Represents the current operational status of an investigator instance.
    /// </summary>
    public enum InvestigatorStatus
    {
        /// <summary>
        /// The investigator is marked as inactive and cannot be started.
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// The investigator is active but not currently running.
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// The investigator is currently executing an investigation.
        /// </summary>
        Running = 2,

        /// <summary>
        /// The investigator's last execution failed and requires attention.
        /// </summary>
        Failed = 3
    }
}