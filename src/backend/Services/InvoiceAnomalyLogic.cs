using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Pure business logic implementation for invoice anomaly detection.
    /// No infrastructure dependencies - can be unit tested independently.
    /// Implements business rules extracted from InvoiceInvestigator.
    /// </summary>
    public class InvoiceAnomalyLogic : IInvestigationLogic<Invoice>
    {
        /// <summary>
        /// Evaluates invoice business rules to find anomalies.
        /// Current rules: negative amounts, excessive tax ratios, future dates.
        /// </summary>
        public IEnumerable<Invoice> FindAnomalies(IEnumerable<Invoice> invoices, IInvestigationConfiguration configuration)
        {
            var config = configuration.Invoice;
            var results = new List<Invoice>();

            foreach (var invoice in invoices)
            {
                if (IsAnomaly(invoice, configuration))
                {
                    results.Add(invoice);
                }
            }

            return results;
        }

        /// <summary>
        /// Evaluates a single invoice against business rules.
        /// </summary>
        public bool IsAnomaly(Invoice invoice, IInvestigationConfiguration configuration)
        {
            var config = configuration.Invoice;

            // Rule 1: Check negative amounts (if enabled)
            if (config.CheckNegativeAmounts && invoice.TotalAmount < 0)
            {
                return true;
            }

            // Rule 2: Check excessive tax ratio (if tax ratio exceeds threshold)
            if (invoice.TotalAmount > 0 && invoice.TotalTax > invoice.TotalAmount * config.MaxTaxRatio)
            {
                return true;
            }

            // Rule 3: Check future dates (if enabled)
            if (config.CheckFutureDates)
            {
                var maxAllowedDate = DateTime.UtcNow.AddDays(config.MaxFutureDays);
                if (invoice.IssueDate > maxAllowedDate)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets detailed reasons why an invoice is considered anomalous.
        /// Useful for audit trails and user feedback.
        /// </summary>
        public IEnumerable<string> GetAnomalyReasons(Invoice invoice, IInvestigationConfiguration configuration)
        {
            var config = configuration.Invoice;
            var reasons = new List<string>();

            // Rule 1: Negative amount check
            if (config.CheckNegativeAmounts && invoice.TotalAmount < 0)
            {
                reasons.Add($"Negative total amount: {invoice.TotalAmount:C}");
            }

            // Rule 2: Excessive tax ratio check
            if (invoice.TotalAmount > 0 && invoice.TotalTax > invoice.TotalAmount * config.MaxTaxRatio)
            {
                var actualRatio = invoice.TotalTax / invoice.TotalAmount;
                var maxRatioPercent = config.MaxTaxRatio * 100;
                var actualRatioPercent = actualRatio * 100;
                reasons.Add($"Excessive tax ratio: {actualRatioPercent:F1}% (max allowed: {maxRatioPercent:F1}%)");
            }

            // Rule 3: Future date check
            if (config.CheckFutureDates)
            {
                var maxAllowedDate = DateTime.UtcNow.AddDays(config.MaxFutureDays);
                if (invoice.IssueDate > maxAllowedDate)
                {
                    var futureDays = (invoice.IssueDate - DateTime.UtcNow).Days;
                    reasons.Add($"Future issue date: {invoice.IssueDate:yyyy-MM-dd} ({futureDays} days in future, max allowed: {config.MaxFutureDays})");
                }
            }

            return reasons;
        }

        /// <summary>
        /// Performs comprehensive evaluation of an invoice with detailed results.
        /// Useful for detailed analysis and reporting.
        /// </summary>
        public InvestigationResult<Invoice> EvaluateInvoice(Invoice invoice, IInvestigationConfiguration configuration)
        {
            var reasons = GetAnomalyReasons(invoice, configuration).ToList();
            
            return new InvestigationResult<Invoice>
            {
                Entity = invoice,
                IsAnomaly = reasons.Any(),
                Reasons = reasons,
                EvaluatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Batch evaluation of invoices with detailed results.
        /// More efficient than individual evaluations for large datasets.
        /// </summary>
        public IEnumerable<InvestigationResult<Invoice>> EvaluateInvoices(IEnumerable<Invoice> invoices, IInvestigationConfiguration configuration)
        {
            return invoices.Select(invoice => EvaluateInvoice(invoice, configuration));
        }

        /// <summary>
        /// Gets anomaly statistics for a collection of invoices.
        /// Useful for dashboard and reporting purposes.
        /// </summary>
        public InvoiceAnomalyStatistics GetAnomalyStatistics(IEnumerable<Invoice> invoices, IInvestigationConfiguration configuration)
        {
            var config = configuration.Invoice;
            var stats = new InvoiceAnomalyStatistics();
            
            foreach (var invoice in invoices)
            {
                stats.TotalInvoices++;
                
                if (IsAnomaly(invoice, configuration))
                {
                    stats.TotalAnomalies++;
                    var reasons = GetAnomalyReasons(invoice, configuration);
                    
                    foreach (var reason in reasons)
                    {
                        if (reason.Contains("Negative total amount"))
                            stats.NegativeAmountCount++;
                        else if (reason.Contains("Excessive tax ratio"))
                            stats.ExcessiveTaxCount++;
                        else if (reason.Contains("Future issue date"))
                            stats.FutureDateCount++;
                    }
                }
            }
            
            return stats;
        }
    }

    /// <summary>
    /// Statistical summary of invoice anomalies.
    /// Provides insights for business analysis and reporting.
    /// </summary>
    public class InvoiceAnomalyStatistics
    {
        /// <summary>Total number of invoices evaluated.</summary>
        public int TotalInvoices { get; set; }
        
        /// <summary>Total number of anomalous invoices found.</summary>
        public int TotalAnomalies { get; set; }
        
        /// <summary>Number of invoices with negative amounts.</summary>
        public int NegativeAmountCount { get; set; }
        
        /// <summary>Number of invoices with excessive tax ratios.</summary>
        public int ExcessiveTaxCount { get; set; }
        
        /// <summary>Number of invoices with future dates.</summary>
        public int FutureDateCount { get; set; }
        
        /// <summary>Anomaly rate as a percentage.</summary>
        public double AnomalyRate => TotalInvoices > 0 ? (double)TotalAnomalies / TotalInvoices * 100 : 0;
    }
}