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
    /// Investigator responsible for processing waybills using extracted business logic.
    /// Now uses pure business logic components for delivery issue detection.
    /// Separated concerns: data access, business rules, and result recording.
    /// </summary>
    public class WaybillInvestigator : Investigator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly WaybillDeliveryLogic _businessLogic;
        private readonly IInvestigationConfiguration _configuration;
        private readonly StreamingConfiguration _streamingConfig;

        /// <summary>
        /// Initializes a new instance with injected business logic and configuration.
        /// </summary>
        /// <param name="dbFactory">Database context factory for data access.</param>
        /// <param name="businessLogic">Pure business logic for delivery issue detection.</param>
        /// <param name="configuration">Business rule configuration.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        /// <param name="streamingConfig">Streaming performance configuration.</param>
        public WaybillInvestigator(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            WaybillDeliveryLogic businessLogic,
            IInvestigationConfiguration configuration,
            StreamingConfiguration streamingConfig,
            ILogger<WaybillInvestigator>? logger)
            : base("Waybill Investigator", logger)
        {
            _dbFactory = dbFactory;
            _businessLogic = businessLogic;
            _configuration = configuration;
            _streamingConfig = streamingConfig ?? new StreamingConfiguration();
        }

        /// <summary>
        /// Executes waybill investigation using streaming optimization.
        /// Uses IAsyncEnumerable for memory-efficient processing of large datasets.
        /// </summary>
        protected override void OnInvestigate()
        {
            if (_streamingConfig.EnableStreamingOptimization)
            {
                OnInvestigateStreaming();
            }
            else
            {
                OnInvestigateLegacy();
            }
        }

        /// <summary>
        /// Streaming-optimized investigation for large waybill datasets.
        /// Processes data in batches to minimize memory usage.
        /// FIXED: Now waits for async work to complete before returning to prevent race conditions.
        /// </summary>
        private void OnInvestigateStreaming()
        {
            // CRITICAL FIX: Wait for async work to complete before returning
            // This ensures all results are saved before the manager marks completion
            Task.Run(async () => await OnInvestigateStreamingAsync()).GetAwaiter().GetResult();
        }

        private async Task OnInvestigateStreamingAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            var repository = new GenericRepository<Waybill>(db);
            
            var waybillBatch = new List<Waybill>(_streamingConfig.WaybillBatchSize);
            var totalProcessed = 0;
            var totalAnomalies = 0;
            
            try
            {
                await foreach (var waybill in repository.GetAllStreamAsync())
                {
                    waybillBatch.Add(waybill);
                    
                    // Process batch when full
                    if (waybillBatch.Count >= _streamingConfig.WaybillBatchSize)
                    {
                        var batchResults = ProcessWaybillBatch(waybillBatch);
                        totalProcessed += waybillBatch.Count;
                        totalAnomalies += batchResults;
                        
                        waybillBatch.Clear();
                        
                        // Optional: Log progress for large datasets
                        if (_streamingConfig.EnablePerformanceMetrics && totalProcessed % (_streamingConfig.WaybillBatchSize * 5) == 0)
                        {
                            _logger.LogInformation($"Processed {totalProcessed} waybills, found {totalAnomalies} anomalies");
                        }
                    }
                }
                
                // Process remaining waybills in final batch
                if (waybillBatch.Count > 0)
                {
                    var finalBatchResults = ProcessWaybillBatch(waybillBatch);
                    totalProcessed += waybillBatch.Count;
                    totalAnomalies += finalBatchResults;
                }
                
                // Record overall statistics
                RecordStreamingStatistics(totalProcessed, totalAnomalies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during streaming waybill investigation");
                RecordResult($"Streaming investigation failed: {ex.Message}", JsonSerializer.Serialize(new { Error = ex.Message, ProcessedCount = totalProcessed }), ResultSeverity.Anomaly);
            }
        }
        
        /// <summary>
        /// Legacy investigation method for fallback compatibility.
        /// </summary>
        private void OnInvestigateLegacy()
        {
            using var db = _dbFactory.CreateDbContext();
            
            // Data Access: Get all waybills from database
            var waybills = db.Waybills.ToList();
            
            // Business Logic: Evaluate waybills using pure business logic
            var results = _businessLogic.EvaluateWaybills(waybills, _configuration);
            
            // Result Recording: Process and record findings
            foreach (var result in results.Where(r => r.IsAnomaly))
            {
                var waybill = result.Entity;
                var reasonsText = string.Join(", ", result.Reasons);
                
                // Determine the primary issue type for classification
                var issueType = DetermineIssueType(result.Reasons);
                var resultMessage = $"{issueType.ToUpper()}: Waybill {waybill.Id} - {reasonsText}";
                
                // Enhanced result payload with detailed information
                var resultPayload = new
                {
                    waybill.Id,
                    waybill.RecipientName,
                    waybill.GoodsIssueDate,
                    waybill.DueDate,
                    IssueType = issueType,
                    DeliveryReasons = result.Reasons,
                    EvaluatedAt = result.EvaluatedAt,
                    Configuration = new
                    {
                        _configuration.Waybill.ExpiringSoonHours,
                        _configuration.Waybill.LegacyCutoffDays,
                        _configuration.Waybill.CheckOverdueDeliveries,
                        _configuration.Waybill.CheckExpiringSoon,
                        _configuration.Waybill.CheckLegacyWaybills
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload), ResultSeverity.Anomaly);
            }
            
            // Enhanced Statistics: Record comprehensive statistics for monitoring
            var stats = _businessLogic.GetDeliveryStatistics(waybills, _configuration);
            if (stats.TotalWaybills > 0)
            {
                var statsMessage = $"Investigation complete: {stats.TotalProblematic}/{stats.TotalWaybills} issues found ({stats.ProblematicRate:F1}%)";
                var statsPayload = new
                {
                    stats.TotalWaybills,
                    stats.TotalProblematic,
                    stats.ProblematicRate,
                    DeliveryBreakdown = new
                    {
                        stats.OverdueCount,
                        stats.ExpiringSoonCount,
                        stats.LegacyOverdueCount
                    },
                    WaybillTypes = new
                    {
                        stats.WithDueDateCount,
                        stats.LegacyWaybillCount
                    },
                    CompletedAt = DateTime.UtcNow,
                    ConfigurationApplied = new
                    {
                        _configuration.Waybill.ExpiringSoonHours,
                        _configuration.Waybill.LegacyCutoffDays
                    }
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }

            // Optional: Record specialized category summaries for dashboard purposes
            RecordSpecializedSummaries(waybills);
        }
        
        /// <summary>
        /// Processes a batch of waybills and records anomalies.
        /// Returns the number of anomalies found in this batch.
        /// </summary>
        private int ProcessWaybillBatch(List<Waybill> waybillBatch)
        {
            var results = _businessLogic.EvaluateWaybills(waybillBatch, _configuration);
            var anomalyCount = 0;
            
            foreach (var result in results.Where(r => r.IsAnomaly))
            {
                var waybill = result.Entity;
                var reasonsText = string.Join(", ", result.Reasons);
                
                // Determine the primary issue type for classification
                var issueType = DetermineIssueType(result.Reasons);
                var resultMessage = $"{issueType.ToUpper()}: Waybill {waybill.Id} - {reasonsText}";
                
                // Enhanced result payload with detailed information
                var resultPayload = new
                {
                    waybill.Id,
                    waybill.RecipientName,
                    waybill.GoodsIssueDate,
                    waybill.DueDate,
                    IssueType = issueType,
                    DeliveryReasons = result.Reasons,
                    EvaluatedAt = result.EvaluatedAt,
                    ProcessingMode = "Streaming",
                    BatchSize = waybillBatch.Count,
                    Configuration = new
                    {
                        _configuration.Waybill.ExpiringSoonHours,
                        _configuration.Waybill.LegacyCutoffDays,
                        _configuration.Waybill.CheckOverdueDeliveries,
                        _configuration.Waybill.CheckExpiringSoon,
                        _configuration.Waybill.CheckLegacyWaybills
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload), ResultSeverity.Anomaly);
                anomalyCount++;
            }
            
            return anomalyCount;
        }
        
        /// <summary>
        /// Records comprehensive statistics for streaming processing.
        /// </summary>
        private void RecordStreamingStatistics(int totalProcessed, int totalAnomalies)
        {
            if (totalProcessed > 0)
            {
                var anomalyRate = (double)totalAnomalies / totalProcessed * 100;
                var statsMessage = $"Streaming investigation complete: {totalAnomalies}/{totalProcessed} issues found ({anomalyRate:F1}%)";
                var statsPayload = new
                {
                    TotalWaybills = totalProcessed,
                    TotalAnomalies = totalAnomalies,
                    AnomalyRate = anomalyRate,
                    ProcessingMode = "Streaming",
                    BatchSize = _streamingConfig.WaybillBatchSize,
                    CompletedAt = DateTime.UtcNow,
                    PerformanceOptimization = new
                    {
                        StreamingEnabled = _streamingConfig.EnableStreamingOptimization,
                        ConfiguredBatchSize = _streamingConfig.WaybillBatchSize,
                        MaxBufferSize = _streamingConfig.MaxBufferSize
                    }
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }
        }

        
        /// <summary>
        /// Records specialized summaries for different delivery issue categories.
        /// Useful for dashboard widgets and alert systems.
        /// </summary>
        private void RecordSpecializedSummaries(List<Waybill> waybills)
        {
            // Overdue summary
            var overdueWaybills = _businessLogic.FindOverdueWaybills(waybills, _configuration).ToList();
            if (overdueWaybills.Any())
            {
                RecordResult($"Overdue Summary: {overdueWaybills.Count} waybills past due date", 
                           JsonSerializer.Serialize(new { 
                               Type = "OverdueSummary",
                               Count = overdueWaybills.Count,
                               WaybillIds = overdueWaybills.Select(w => w.Id).ToList()
                           }));
            }

            // Expiring soon summary
            var expiringSoon = _businessLogic.FindExpiringSoonWaybills(waybills, _configuration).ToList();
            if (expiringSoon.Any())
            {
                RecordResult($"Expiring Soon Summary: {expiringSoon.Count} waybills due within {_configuration.Waybill.ExpiringSoonHours}h", 
                           JsonSerializer.Serialize(new { 
                               Type = "ExpiringSoonSummary",
                               Count = expiringSoon.Count,
                               ThresholdHours = _configuration.Waybill.ExpiringSoonHours,
                               WaybillIds = expiringSoon.Select(w => w.Id).ToList()
                           }));
            }

            // Legacy overdue summary
            var legacyOverdue = _businessLogic.FindLegacyOverdueWaybills(waybills, _configuration).ToList();
            if (legacyOverdue.Any())
            {
                RecordResult($"Legacy Overdue Summary: {legacyOverdue.Count} legacy waybills past {_configuration.Waybill.LegacyCutoffDays}d cutoff", 
                           JsonSerializer.Serialize(new { 
                               Type = "LegacyOverdueSummary",
                               Count = legacyOverdue.Count,
                               CutoffDays = _configuration.Waybill.LegacyCutoffDays,
                               WaybillIds = legacyOverdue.Select(w => w.Id).ToList()
                           }));
            }
        }

        /// <summary>
        /// Determines the primary issue type from anomaly reasons.
        /// Useful for categorization and alerting.
        /// </summary>
        private string DetermineIssueType(IList<string> reasons)
        {
            // Prioritize by severity: Overdue > Expiring Soon > Legacy
            if (reasons.Any(r => r.Contains("Overdue delivery")))
                return "Overdue";
            else if (reasons.Any(r => r.Contains("Expiring soon")))
                return "ExpiringSoon";
            else if (reasons.Any(r => r.Contains("Legacy waybill overdue")))
                return "LegacyLate";
            else
                return "DeliveryIssue";
        }


    }
}
