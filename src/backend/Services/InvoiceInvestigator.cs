using System;
using System.Linq;
using ea_Tracker.Data;
using ea_Tracker.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing invoices using extracted business logic.
    /// Now uses pure business logic components for anomaly detection.
    /// Separated concerns: data access, business rules, and result recording.
    /// </summary>
    public class InvoiceInvestigator : Investigator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly InvoiceAnomalyLogic _businessLogic;
        private readonly IInvestigationConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance with injected business logic and configuration.
        /// </summary>
        /// <param name="dbFactory">Database context factory for data access.</param>
        /// <param name="businessLogic">Pure business logic for anomaly detection.</param>
        /// <param name="configuration">Business rule configuration.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        public InvoiceInvestigator(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            InvoiceAnomalyLogic businessLogic,
            IInvestigationConfiguration configuration,
            ILogger<InvoiceInvestigator>? logger)
            : base("Invoice Investigator", logger)
        {
            _dbFactory = dbFactory;
            _businessLogic = businessLogic;
            _configuration = configuration;
        }

        /// <summary>
        /// Begins invoice investigation operations using pure business logic.
        /// Separates data access from business rule evaluation.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            
            // Data Access: Get all invoices from database
            var invoices = db.Invoices.ToList();
            
            // Business Logic: Evaluate invoices using pure business logic
            var results = _businessLogic.EvaluateInvoices(invoices, _configuration);
            
            // Result Recording: Process and record findings
            foreach (var result in results.Where(r => r.IsAnomaly))
            {
                var invoice = result.Entity;
                var reasonsText = string.Join(", ", result.Reasons);
                var resultMessage = $"Anomalous invoice {invoice.Id}: {reasonsText}";
                
                // Enhanced result payload with detailed information
                var resultPayload = new
                {
                    invoice.Id,
                    invoice.TotalAmount,
                    invoice.TotalTax,
                    invoice.IssueDate,
                    invoice.RecipientName,
                    AnomalyReasons = result.Reasons,
                    EvaluatedAt = result.EvaluatedAt,
                    Configuration = new
                    {
                        _configuration.Invoice.MaxTaxRatio,
                        _configuration.Invoice.CheckNegativeAmounts,
                        _configuration.Invoice.CheckFutureDates,
                        _configuration.Invoice.MaxFutureDays
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
            }
            
            // Optional: Record statistics for monitoring
            var stats = _businessLogic.GetAnomalyStatistics(invoices, _configuration);
            if (stats.TotalInvoices > 0)
            {
                var statsMessage = $"Investigation complete: {stats.TotalAnomalies}/{stats.TotalInvoices} anomalies found ({stats.AnomalyRate:F1}%)";
                var statsPayload = new
                {
                    stats.TotalInvoices,
                    stats.TotalAnomalies,
                    stats.AnomalyRate,
                    stats.NegativeAmountCount,
                    stats.ExcessiveTaxCount,
                    stats.FutureDateCount,
                    CompletedAt = DateTime.UtcNow
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }
        }

        /// <summary>
        /// Stops invoice investigation operations.
        /// Clean shutdown with no additional logic required.
        /// </summary>
        protected override void OnStop()
        {
            // Clean shutdown - no resources to release
        }

    }
}
