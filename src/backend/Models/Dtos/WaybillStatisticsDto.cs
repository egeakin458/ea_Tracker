namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// Data transfer object for waybill statistics and analytics.
    /// Used by services to return aggregated waybill data for dashboard and reporting.
    /// </summary>
    public class WaybillStatisticsDto
    {
        /// <summary>
        /// Gets or sets the total number of waybills in the system.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of waybills flagged as anomalous.
        /// </summary>
        public int AnomalousCount { get; set; }

        /// <summary>
        /// Gets the percentage of waybills that are anomalous.
        /// Returns 0 if there are no waybills.
        /// </summary>
        public decimal AnomalyRate => TotalCount > 0 ? (decimal)AnomalousCount / TotalCount * 100 : 0;

        /// <summary>
        /// Gets or sets the number of overdue waybills.
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// Gets or sets the number of waybills expiring soon.
        /// </summary>
        public int ExpiringSoonCount { get; set; }

        /// <summary>
        /// Gets or sets the number of legacy waybills (older than cutoff).
        /// </summary>
        public int LegacyCount { get; set; }

        /// <summary>
        /// Gets or sets the number of waybills that are late (past expected delivery).
        /// </summary>
        public int LateDeliveryCount { get; set; }

        /// <summary>
        /// Gets or sets the total weight of all waybills.
        /// </summary>
        public decimal TotalWeight { get; set; }

        /// <summary>
        /// Gets the average waybill weight.
        /// Returns 0 if there are no waybills.
        /// </summary>
        public decimal AverageWeight => TotalCount > 0 ? TotalWeight / TotalCount : 0;

        /// <summary>
        /// Gets or sets the number of waybills with deliveries completed on time.
        /// </summary>
        public int OnTimeDeliveryCount { get; set; }

        /// <summary>
        /// Gets the on-time delivery percentage.
        /// Returns 0 if there are no waybills.
        /// </summary>
        public decimal OnTimeDeliveryRate => TotalCount > 0 ? (decimal)OnTimeDeliveryCount / TotalCount * 100 : 0;
    }
}