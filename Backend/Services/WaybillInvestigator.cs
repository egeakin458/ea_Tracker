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
        /// Begins waybill investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var late = db.Waybills
                .Where(w => w.GoodsIssueDate < cutoff)
                .ToList();

            foreach (var w in late)
            {
                RecordResult($"Late waybill {w.Id}", JsonSerializer.Serialize(w));
            }
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
