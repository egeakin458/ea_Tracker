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
    /// Investigator responsible for processing waybills using extracted business logic.
    /// Now uses pure business logic components for delivery issue detection.
    /// Separated concerns: data access, business rules, and result recording.
    /// </summary>
    public class WaybillInvestigator : Investigator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly WaybillDeliveryLogic _businessLogic;
        private readonly IInvestigationConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance with injected business logic and configuration.
        /// </summary>
        /// <param name="dbFactory">Database context factory for data access.</param>
        /// <param name="businessLogic">Pure business logic for delivery issue detection.</param>
        /// <param name="configuration">Business rule configuration.</param>
        /// <param name="logger">Logger for diagnostic information.</param>
        public WaybillInvestigator(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            WaybillDeliveryLogic businessLogic,
            IInvestigationConfiguration configuration,
            ILogger<WaybillInvestigator>? logger)
            : base("Waybill Investigator", logger)
        {
            _dbFactory = dbFactory;
            _businessLogic = businessLogic;
            _configuration = configuration;
        }

        /// <summary>
        /// Executes waybill investigation using pure business logic.
        /// Separates data access from business rule evaluation.
        /// </summary>
        protected override void OnInvestigate()
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
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
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
