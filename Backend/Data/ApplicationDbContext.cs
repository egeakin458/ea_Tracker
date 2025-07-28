using Microsoft.EntityFrameworkCore;
using ea_Tracker.Models;

namespace ea_Tracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets here. For example:
        // public DbSet<YourEntity> YourEntities { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Waybill> Waybills { get; set; }

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
