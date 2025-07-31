using System;
using System.Linq;
using ea_Tracker.Data;
using ea_Tracker.Models;
using System.Text.Json;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing invoices.
    /// </summary>
    public class InvoiceInvestigator : Investigator
    {
        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceInvestigator"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        public InvoiceInvestigator(ApplicationDbContext db) : base("Invoice Investigator")
        {
            _db = db;
        }

        /// <summary>
        /// Begins invoice investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            var anomalies = _db.Invoices
                .Where(i => i.TotalAmount < 0 ||
                            i.TotalTax > i.TotalAmount * 0.5m ||
                            i.IssueDate > DateTime.UtcNow)
                .ToList();

            foreach (var a in anomalies)
            {
                RecordResult($"Anomalous invoice {a.Id}", JsonSerializer.Serialize(a));
            }
        }

        /// <summary>
        /// Stops invoice investigation operations.
        /// </summary>
        protected override void OnStop()
        {
        }
    }
}
