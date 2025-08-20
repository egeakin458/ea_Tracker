using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ea_Tracker.Services.Performance;
using ea_Tracker.Repositories;

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
        private readonly StreamingConfiguration _streamingConfig;

        /// <summary>
        /// Initializes a new instance with injected business logic and configuration.
        /// </summary>
        /// <param name="dbFactory">Database context factory for data access.</param>
        /// <param name="businessLogic">Pure business logic for anomaly detection.</param>
        /// <param name="configuration">Business rule configuration.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        /// <param name="streamingConfig">Streaming performance configuration.</param>
        public InvoiceInvestigator(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            InvoiceAnomalyLogic businessLogic,
            IInvestigationConfiguration configuration,
            StreamingConfiguration streamingConfig,
            ILogger<InvoiceInvestigator>? logger)
            : base("Invoice Investigator", logger)
        {
            _dbFactory = dbFactory;
            _businessLogic = businessLogic;
            _configuration = configuration;
            _streamingConfig = streamingConfig ?? new StreamingConfiguration();
        }

        /// <summary>
        /// Executes invoice investigation with enhanced streaming optimization.
        /// Uses improved batching for better memory efficiency.
        /// </summary>
        protected override void OnInvestigate()
        {
            if (_streamingConfig.EnableStreamingOptimization)
            {
                OnInvestigateStreamingEnhanced();
            }
            else
            {
                OnInvestigateLegacy();
            }
        }

        /// <summary>
        /// Enhanced streaming investigation with improved batching.
        /// Builds upon existing batch processing patterns.
        /// FIXED: Now waits for async work to complete before returning to prevent race conditions.
        /// </summary>
        private void OnInvestigateStreamingEnhanced()
        {
            // CRITICAL FIX: Wait for async work to complete before returning
            // This ensures all results are saved before the manager marks completion
            Task.Run(async () => await OnInvestigateStreamingEnhancedAsync()).GetAwaiter().GetResult();
        }

        private async Task OnInvestigateStreamingEnhancedAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            var repository = new GenericRepository<Invoice>(db);
            
            var invoiceBatch = new List<Invoice>(_streamingConfig.InvoiceBatchSize);
            var totalProcessed = 0;
            var totalAnomalies = 0;
            
            try
            {
                await foreach (var invoice in repository.GetAllStreamAsync())
                {
                    invoiceBatch.Add(invoice);
                    
                    // Process batch when full
                    if (invoiceBatch.Count >= _streamingConfig.InvoiceBatchSize)
                    {
                        var batchResults = ProcessInvoiceBatch(invoiceBatch);
                        totalProcessed += invoiceBatch.Count;
                        totalAnomalies += batchResults;
                        
                        invoiceBatch.Clear();
                        
                        // Optional: Log progress for large datasets
                        if (_streamingConfig.EnablePerformanceMetrics && totalProcessed % (_streamingConfig.InvoiceBatchSize * 5) == 0)
                        {
                            _logger.LogInformation($"Processed {totalProcessed} invoices, found {totalAnomalies} anomalies");
                        }
                    }
                }
                
                // Process remaining invoices in final batch
                if (invoiceBatch.Count > 0)
                {
                    var finalBatchResults = ProcessInvoiceBatch(invoiceBatch);
                    totalProcessed += invoiceBatch.Count;
                    totalAnomalies += finalBatchResults;
                }
                
                // Record enhanced statistics
                RecordEnhancedStatistics(totalProcessed, totalAnomalies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enhanced streaming invoice investigation");
                RecordResult($"Enhanced streaming investigation failed: {ex.Message}", JsonSerializer.Serialize(new { Error = ex.Message, ProcessedCount = totalProcessed }), ResultSeverity.Anomaly);
            }
        }
        
        /// <summary>
        /// Legacy investigation method for fallback compatibility.
        /// </summary>
        private void OnInvestigateLegacy()
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
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload), ResultSeverity.Anomaly);
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
        /// Processes a batch of invoices and records anomalies.
        /// Returns the number of anomalies found in this batch.
        /// </summary>
        private int ProcessInvoiceBatch(List<Invoice> invoiceBatch)
        {
            var results = _businessLogic.EvaluateInvoices(invoiceBatch, _configuration);
            var anomalyCount = 0;
            
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
                    ProcessingMode = "EnhancedStreaming",
                    BatchSize = invoiceBatch.Count,
                    Configuration = new
                    {
                        _configuration.Invoice.MaxTaxRatio,
                        _configuration.Invoice.CheckNegativeAmounts,
                        _configuration.Invoice.CheckFutureDates,
                        _configuration.Invoice.MaxFutureDays
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload), ResultSeverity.Anomaly);
                anomalyCount++;
            }
            
            return anomalyCount;
        }
        
        /// <summary>
        /// Records enhanced statistics for streaming processing.
        /// </summary>
        private void RecordEnhancedStatistics(int totalProcessed, int totalAnomalies)
        {
            if (totalProcessed > 0)
            {
                var anomalyRate = (double)totalAnomalies / totalProcessed * 100;
                var statsMessage = $"Enhanced streaming investigation complete: {totalAnomalies}/{totalProcessed} anomalies found ({anomalyRate:F1}%)";
                var statsPayload = new
                {
                    TotalInvoices = totalProcessed,
                    TotalAnomalies = totalAnomalies,
                    AnomalyRate = anomalyRate,
                    ProcessingMode = "EnhancedStreaming",
                    BatchSize = _streamingConfig.InvoiceBatchSize,
                    CompletedAt = DateTime.UtcNow,
                    PerformanceOptimization = new
                    {
                        StreamingEnabled = _streamingConfig.EnableStreamingOptimization,
                        ConfiguredBatchSize = _streamingConfig.InvoiceBatchSize,
                        MaxBufferSize = _streamingConfig.MaxBufferSize,
                        EnhancedBatching = true
                    }
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }
        }


    }
}
