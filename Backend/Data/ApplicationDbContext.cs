using Microsoft.EntityFrameworkCore;
using ea_Tracker.Models;

namespace ea_Tracker.Data
{
    /// <summary>
    /// Entity Framework database context for the application.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets here. For example:
        // public DbSet<YourEntity> YourEntities { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Waybill> Waybills { get; set; }

        /// <summary>
        /// Configures the schema needed for the context.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceType)
                .HasConversion<string>();

            modelBuilder.Entity<Waybill>()
                .Property(w => w.WaybillType)
                .HasConversion<string>();
        }
    }
}
