namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// Data transfer object for invoice statistics and analytics.
    /// Used by services to return aggregated invoice data for dashboard and reporting.
    /// </summary>
    public class InvoiceStatisticsDto
    {
        /// <summary>
        /// Gets or sets the total number of invoices in the system.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of invoices flagged as anomalous.
        /// </summary>
        public int AnomalousCount { get; set; }

        /// <summary>
        /// Gets the percentage of invoices that are anomalous.
        /// Returns 0 if there are no invoices.
        /// </summary>
        public decimal AnomalyRate => TotalCount > 0 ? (decimal)AnomalousCount / TotalCount * 100 : 0;

        /// <summary>
        /// Gets or sets the sum of all invoice amounts.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the average tax ratio across all invoices.
        /// </summary>
        public decimal AverageTaxRatio { get; set; }

        /// <summary>
        /// Gets or sets the number of invoices with negative amounts.
        /// </summary>
        public int NegativeAmountCount { get; set; }

        /// <summary>
        /// Gets or sets the number of invoices with future dates.
        /// </summary>
        public int FutureDatedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of invoices with tax ratios exceeding the configured threshold.
        /// </summary>
        public int HighTaxRatioCount { get; set; }

        /// <summary>
        /// Gets the average invoice amount.
        /// Returns 0 if there are no invoices.
        /// </summary>
        public decimal AverageAmount => TotalCount > 0 ? TotalAmount / TotalCount : 0;
    }
}