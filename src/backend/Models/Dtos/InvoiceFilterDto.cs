namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// Data transfer object for filtering invoice queries.
    /// Used by services to apply filtering criteria in GetAll operations.
    /// </summary>
    public class InvoiceFilterDto
    {
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
        /// Gets or sets the minimum total amount filter.
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Gets or sets the maximum total amount filter.
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Gets or sets the anomaly status filter.
        /// Null = all invoices, true = anomalous only, false = normal only.
        /// </summary>
        public bool? HasAnomalies { get; set; }

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