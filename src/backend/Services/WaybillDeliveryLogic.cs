using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Pure business logic implementation for waybill delivery investigation.
    /// No infrastructure dependencies - can be unit tested independently.
    /// Implements enhanced algorithms with configurable thresholds.
    /// </summary>
    public class WaybillDeliveryLogic : IInvestigationLogic<Waybill>
    {
        /// <summary>
        /// Evaluates waybill delivery rules to find delivery issues.
        /// Current rules: overdue deliveries, expiring soon, legacy cutoff.
        /// </summary>
        public IEnumerable<Waybill> FindAnomalies(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            var config = configuration.Waybill;
            var results = new List<Waybill>();

            foreach (var waybill in waybills)
            {
                if (IsAnomaly(waybill, configuration))
                {
                    results.Add(waybill);
                }
            }

            return results;
        }

        /// <summary>
        /// Evaluates a single waybill against delivery business rules.
        /// </summary>
        public bool IsAnomaly(Waybill waybill, IInvestigationConfiguration configuration)
        {
            var config = configuration.Waybill;
            var now = DateTime.UtcNow;

            // Rule 1: Check overdue deliveries (past due date)
            if (config.CheckOverdueDeliveries && waybill.DueDate.HasValue && waybill.DueDate.Value < now)
            {
                return true;
            }

            // Rule 2: Check expiring soon deliveries  
            if (config.CheckExpiringSoon && waybill.DueDate.HasValue)
            {
                var expiringSoonThreshold = now.AddHours(config.ExpiringSoonHours);
                if (waybill.DueDate.Value <= expiringSoonThreshold && waybill.DueDate.Value >= now)
                {
                    return true;
                }
            }

            // Rule 3: Check legacy waybills (those without due dates using goods issue date)
            if (config.CheckLegacyWaybills && !waybill.DueDate.HasValue)
            {
                var legacyCutoff = now.AddDays(-config.LegacyCutoffDays);
                if (waybill.GoodsIssueDate < legacyCutoff)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets detailed reasons why a waybill is considered problematic.
        /// </summary>
        public IEnumerable<string> GetAnomalyReasons(Waybill waybill, IInvestigationConfiguration configuration)
        {
            var config = configuration.Waybill;
            var reasons = new List<string>();
            var now = DateTime.UtcNow;

            // Rule 1: Overdue delivery check
            if (config.CheckOverdueDeliveries && waybill.DueDate.HasValue && waybill.DueDate.Value < now)
            {
                var overdueDays = (now - waybill.DueDate.Value).Days;
                var overdueHours = (now - waybill.DueDate.Value).Hours % 24;
                reasons.Add($"Overdue delivery: {overdueDays}d {overdueHours}h past due date ({waybill.DueDate.Value:yyyy-MM-dd HH:mm})");
            }

            // Rule 2: Expiring soon check
            if (config.CheckExpiringSoon && waybill.DueDate.HasValue)
            {
                var expiringSoonThreshold = now.AddHours(config.ExpiringSoonHours);
                if (waybill.DueDate.Value <= expiringSoonThreshold && waybill.DueDate.Value >= now)
                {
                    var remainingHours = (waybill.DueDate.Value - now).TotalHours;
                    reasons.Add($"Expiring soon: {remainingHours:F1}h remaining (threshold: {config.ExpiringSoonHours}h)");
                }
            }

            // Rule 3: Legacy waybill check
            if (config.CheckLegacyWaybills && !waybill.DueDate.HasValue)
            {
                var legacyCutoff = now.AddDays(-config.LegacyCutoffDays);
                if (waybill.GoodsIssueDate < legacyCutoff)
                {
                    var daysSinceIssue = (now - waybill.GoodsIssueDate).Days;
                    reasons.Add($"Legacy waybill overdue: {daysSinceIssue} days since goods issue (cutoff: {config.LegacyCutoffDays} days)");
                }
            }

            return reasons;
        }

        /// <summary>
        /// Performs comprehensive evaluation of a waybill with detailed results.
        /// </summary>
        public InvestigationResult<Waybill> EvaluateWaybill(Waybill waybill, IInvestigationConfiguration configuration)
        {
            var reasons = GetAnomalyReasons(waybill, configuration).ToList();
            
            return new InvestigationResult<Waybill>
            {
                Entity = waybill,
                IsAnomaly = reasons.Any(),
                Reasons = reasons,
                EvaluatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Batch evaluation of waybills with detailed results.
        /// </summary>
        public IEnumerable<InvestigationResult<Waybill>> EvaluateWaybills(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            return waybills.Select(waybill => EvaluateWaybill(waybill, configuration));
        }

        /// <summary>
        /// Specialized method to find only overdue waybills.
        /// More efficient than general anomaly detection when only overdue items are needed.
        /// </summary>
        public IEnumerable<Waybill> FindOverdueWaybills(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            if (!configuration.Waybill.CheckOverdueDeliveries)
                return Enumerable.Empty<Waybill>();

            var now = DateTime.UtcNow;
            
            return waybills.Where(w => 
                w.DueDate.HasValue && w.DueDate.Value < now);
        }

        /// <summary>
        /// Specialized method to find waybills expiring soon.
        /// </summary>
        public IEnumerable<Waybill> FindExpiringSoonWaybills(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            if (!configuration.Waybill.CheckExpiringSoon)
                return Enumerable.Empty<Waybill>();

            var now = DateTime.UtcNow;
            var expiringSoonThreshold = now.AddHours(configuration.Waybill.ExpiringSoonHours);
            
            return waybills.Where(w => 
                w.DueDate.HasValue && 
                w.DueDate.Value >= now && 
                w.DueDate.Value <= expiringSoonThreshold);
        }

        /// <summary>
        /// Specialized method to find legacy waybills past cutoff.
        /// </summary>
        public IEnumerable<Waybill> FindLegacyOverdueWaybills(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            if (!configuration.Waybill.CheckLegacyWaybills)
                return Enumerable.Empty<Waybill>();

            var now = DateTime.UtcNow;
            var legacyCutoff = now.AddDays(-configuration.Waybill.LegacyCutoffDays);
            
            return waybills.Where(w => 
                !w.DueDate.HasValue && 
                w.GoodsIssueDate < legacyCutoff);
        }

        /// <summary>
        /// Gets delivery statistics for a collection of waybills.
        /// </summary>
        public WaybillDeliveryStatistics GetDeliveryStatistics(IEnumerable<Waybill> waybills, IInvestigationConfiguration configuration)
        {
            var config = configuration.Waybill;
            var stats = new WaybillDeliveryStatistics();
            
            foreach (var waybill in waybills)
            {
                stats.TotalWaybills++;
                
                if (IsAnomaly(waybill, configuration))
                {
                    stats.TotalProblematic++;
                    var reasons = GetAnomalyReasons(waybill, configuration);
                    
                    foreach (var reason in reasons)
                    {
                        if (reason.Contains("Overdue delivery"))
                            stats.OverdueCount++;
                        else if (reason.Contains("Expiring soon"))
                            stats.ExpiringSoonCount++;
                        else if (reason.Contains("Legacy waybill overdue"))
                            stats.LegacyOverdueCount++;
                    }
                }
                
                // Track waybill types
                if (waybill.DueDate.HasValue)
                    stats.WithDueDateCount++;
                else
                    stats.LegacyWaybillCount++;
            }
            
            return stats;
        }
    }

    /// <summary>
    /// Statistical summary of waybill delivery issues.
    /// </summary>
    public class WaybillDeliveryStatistics
    {
        /// <summary>Total number of waybills evaluated.</summary>
        public int TotalWaybills { get; set; }
        
        /// <summary>Total number of problematic waybills found.</summary>
        public int TotalProblematic { get; set; }
        
        /// <summary>Number of overdue waybills (past due date).</summary>
        public int OverdueCount { get; set; }
        
        /// <summary>Number of waybills expiring soon.</summary>
        public int ExpiringSoonCount { get; set; }
        
        /// <summary>Number of legacy waybills past cutoff.</summary>
        public int LegacyOverdueCount { get; set; }
        
        /// <summary>Number of waybills with due dates.</summary>
        public int WithDueDateCount { get; set; }
        
        /// <summary>Number of legacy waybills (without due dates).</summary>
        public int LegacyWaybillCount { get; set; }
        
        /// <summary>Problem rate as a percentage.</summary>
        public double ProblematicRate => TotalWaybills > 0 ? (double)TotalProblematic / TotalWaybills * 100 : 0;
    }
}