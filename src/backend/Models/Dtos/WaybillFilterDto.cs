namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// Data transfer object for filtering waybill queries.
    /// Used by services to apply filtering criteria in GetAll operations.
    /// </summary>
    public class WaybillFilterDto
    {
        /// <summary>
        /// Gets or sets the sender name filter (partial match).
        /// </summary>
        public string? SenderName { get; set; }

        /// <summary>
        /// Gets or sets the recipient name filter (partial match).
        /// </summary>
        public string? RecipientName { get; set; }

        /// <summary>
        /// Gets or sets the minimum issue date filter.
        /// </summary>
        public DateTime? FromIssueDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum issue date filter.
        /// </summary>
        public DateTime? ToIssueDate { get; set; }

        /// <summary>
        /// Gets or sets the minimum expected delivery date filter.
        /// </summary>
        public DateTime? FromExpectedDeliveryDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum expected delivery date filter.
        /// </summary>
        public DateTime? ToExpectedDeliveryDate { get; set; }

        /// <summary>
        /// Gets or sets the minimum weight filter.
        /// </summary>
        public decimal? MinWeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum weight filter.
        /// </summary>
        public decimal? MaxWeight { get; set; }

        /// <summary>
        /// Gets or sets the anomaly status filter.
        /// Null = all waybills, true = anomalous only, false = normal only.
        /// </summary>
        public bool? HasAnomalies { get; set; }

        /// <summary>
        /// Gets or sets the delivery status filter.
        /// True = delivered, false = pending, null = all.
        /// </summary>
        public bool? IsDelivered { get; set; }

        /// <summary>
        /// Gets or sets the overdue status filter.
        /// True = overdue only, false = on-time only, null = all.
        /// </summary>
        public bool? IsOverdue { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        public int? Take { get; set; }

        /// <summary>
        /// Gets or sets the number of results to skip (for pagination).
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets the sort field for ordering results.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction (true = ascending, false = descending).
        /// </summary>
        public bool SortAscending { get; set; } = true;
    }
}