namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// Result of investigation result count verification.
    /// Used to validate accuracy of ResultCount against actual stored results.
    /// </summary>
    public class CountVerificationResult
    {
        /// <summary>
        /// The execution ID being verified.
        /// </summary>
        public int ExecutionId { get; set; }
        
        /// <summary>
        /// The count reported in the InvestigationExecution record.
        /// </summary>
        public int ReportedCount { get; set; }
        
        /// <summary>
        /// The actual count of InvestigationResult records in the database.
        /// </summary>
        public int ActualCount { get; set; }
        
        /// <summary>
        /// Whether the reported count matches the actual count.
        /// </summary>
        public bool IsAccurate { get; set; }
        
        /// <summary>
        /// The difference between actual and reported counts (ActualCount - ReportedCount).
        /// Positive values indicate under-reporting, negative values indicate over-reporting.
        /// </summary>
        public int Discrepancy { get; set; }
        
        /// <summary>
        /// Timestamp when the verification was performed.
        /// </summary>
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }
}