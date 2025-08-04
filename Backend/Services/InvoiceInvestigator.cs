using System;
using System.Linq;
using ea_Tracker.Data;
using ea_Tracker.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Investigator responsible for processing invoices.
    /// </summary>
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

    public class InvoiceInvestigator : Investigator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceInvestigator"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <summary>
        /// Initializes a new instance with the given database and logger.
        /// </summary>
        public InvoiceInvestigator(IDbContextFactory<ApplicationDbContext> dbFactory,
                                   ILogger<InvoiceInvestigator>? logger)
            : base("Invoice Investigator", logger)
        {
            _dbFactory = dbFactory;
        }



        /// <summary>
        /// Begins invoice investigation operations.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            var anomalies = db.Invoices
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
