using System;
using System.Linq;
using ea_Tracker.Data;
using ea_Tracker.Models;
using System.Text.Json;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing waybills.
    /// </summary>
    public class WaybillInvestigator : Investigator
    {
        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaybillInvestigator"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        public WaybillInvestigator(ApplicationDbContext db) : base("Waybill Investigator")
        {
            _db = db;
        }

        /// <summary>
        /// Begins waybill investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var late = _db.Waybills
                .Where(w => w.GoodsIssueDate < cutoff)
                .ToList();

            foreach (var w in late)
            {
                RecordResult($"Late waybill {w.Id}", JsonSerializer.Serialize(w));
            }
        }

        /// <summary>
        /// Stops waybill investigation operations.
        /// </summary>
        protected override void OnStop()
        {
        }
    }
}

