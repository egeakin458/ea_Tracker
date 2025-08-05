namespace ea_Tracker.Enums
{
    /// <summary>
    /// Represents the status of a specific investigation execution session.
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// The execution is currently in progress.
        /// </summary>
        Running = 0,

        /// <summary>
        /// The execution completed successfully.
        /// </summary>
        Completed = 1,

        /// <summary>
        /// The execution encountered an error and failed.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// The execution was cancelled before completion.
        /// </summary>
        Cancelled = 3
    }
}