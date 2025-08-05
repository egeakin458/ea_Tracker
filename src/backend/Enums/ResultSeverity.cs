namespace ea_Tracker.Enums
{
    /// <summary>
    /// Represents the severity level of an investigation result.
    /// </summary>
    public enum ResultSeverity
    {
        /// <summary>
        /// Informational message, no action required.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning that may require attention but is not critical.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error condition that should be addressed.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Anomaly detected that requires investigation.
        /// </summary>
        Anomaly = 3,

        /// <summary>
        /// Critical issue that requires immediate attention.
        /// </summary>
        Critical = 4
    }
}