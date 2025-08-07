using System;
using System.Linq;
using ea_Tracker.Data;
using ea_Tracker.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing waybills.
    /// </summary>
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

    public class WaybillInvestigator : Investigator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaybillInvestigator"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <summary>
        /// Initializes a new instance with the given database and logger.
        /// </summary>
        public WaybillInvestigator(IDbContextFactory<ApplicationDbContext> dbFactory,
                                   ILogger<WaybillInvestigator>? logger)
            : base("Waybill Investigator", logger)
        {
            _dbFactory = dbFactory;
        }



        /// <summary>
        /// Begins waybill investigation operations with enhanced delivery tracking.
        /// Detects overdue deliveries and deliveries expiring soon based on due dates.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            var now = DateTime.UtcNow;
            var expiringThreshold = now.AddDays(1); // 1 day expiring window

            // 1. Find overdue delivery notes (due date has passed)
            var overdueWaybills = db.Waybills
                .Where(w => w.DueDate.HasValue && w.DueDate.Value < now)
                .ToList();

            foreach (var waybill in overdueWaybills)
            {
                var daysPastDue = (now - waybill.DueDate!.Value).TotalDays;
                RecordResult($"OVERDUE: Waybill {waybill.Id} is {daysPastDue:F1} days past due (Due: {waybill.DueDate.Value:yyyy-MM-dd})", 
                           JsonSerializer.Serialize(new { waybill.Id, waybill.RecipientName, waybill.DueDate, DaysPastDue = daysPastDue, Type = "Overdue" }));
            }

            // 2. Find expiring soon delivery notes (due within 1 day)
            var expiringSoonWaybills = db.Waybills
                .Where(w => w.DueDate.HasValue && w.DueDate.Value >= now && w.DueDate.Value <= expiringThreshold)
                .ToList();

            foreach (var waybill in expiringSoonWaybills)
            {
                var hoursUntilDue = (waybill.DueDate!.Value - now).TotalHours;
                RecordResult($"EXPIRING SOON: Waybill {waybill.Id} due in {hoursUntilDue:F1} hours (Due: {waybill.DueDate.Value:yyyy-MM-dd HH:mm})", 
                           JsonSerializer.Serialize(new { waybill.Id, waybill.RecipientName, waybill.DueDate, HoursUntilDue = hoursUntilDue, Type = "ExpiringSoon" }));
            }

            // 3. Legacy logic for waybills without due dates (backward compatibility)
            var cutoff = now.AddDays(-7);
            var legacyLateWaybills = db.Waybills
                .Where(w => !w.DueDate.HasValue && w.GoodsIssueDate < cutoff)
                .ToList();

            foreach (var waybill in legacyLateWaybills)
            {
                var daysLate = (now - waybill.GoodsIssueDate).TotalDays;
                RecordResult($"LEGACY LATE: Waybill {waybill.Id} goods issued {daysLate:F1} days ago (no due date set)", 
                           JsonSerializer.Serialize(new { waybill.Id, waybill.RecipientName, waybill.GoodsIssueDate, DaysLate = daysLate, Type = "LegacyLate" }));
            }

            // Summary report
            RecordResult($"Investigation Summary: {overdueWaybills.Count} overdue, {expiringSoonWaybills.Count} expiring soon, {legacyLateWaybills.Count} legacy late waybills", 
                        JsonSerializer.Serialize(new { 
                            OverdueCount = overdueWaybills.Count,
                            ExpiringSoonCount = expiringSoonWaybills.Count,
                            LegacyLateCount = legacyLateWaybills.Count,
                            TotalIssues = overdueWaybills.Count + expiringSoonWaybills.Count + legacyLateWaybills.Count
                        }));
        }

        /// <summary>
        /// <summary>
        /// Stops waybill investigation operations.
        /// </summary>
        protected override void OnStop()
        {
        }

    }
}
