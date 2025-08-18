using Microsoft.EntityFrameworkCore;
using ea_Tracker.Models;
using ea_Tracker.Enums;

namespace ea_Tracker.Data
{
    /// <summary>
    /// Entity Framework database context for the application.
    /// Enhanced with investigation persistence and optimized configuration.
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

        // Investigation System DbSets
        /// <summary>
        /// Gets or sets the investigator types (reference data) in the database.
        /// </summary>
        public DbSet<InvestigatorType> InvestigatorTypes { get; set; }

        /// <summary>
        /// Gets or sets the investigator instances in the database.
        /// </summary>
        public DbSet<InvestigatorInstance> InvestigatorInstances { get; set; }

        /// <summary>
        /// Gets or sets the investigation executions in the database.
        /// </summary>
        public DbSet<InvestigationExecution> InvestigationExecutions { get; set; }

        /// <summary>
        /// Gets or sets the investigation results in the database.
        /// </summary>
        public DbSet<InvestigationResult> InvestigationResults { get; set; }

        // Business Entity DbSets
        /// <summary>
        /// Gets or sets the invoices in the database.
        /// </summary>
        public DbSet<Invoice> Invoices { get; set; }

        /// <summary>
        /// Gets or sets the waybills in the database.
        /// </summary>
        public DbSet<Waybill> Waybills { get; set; }

        // Authentication System DbSets
        /// <summary>
        /// Gets or sets the users in the database.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the roles in the database.
        /// </summary>
        public DbSet<Role> Roles { get; set; }

        /// <summary>
        /// Gets or sets the user-role assignments in the database.
        /// </summary>
        public DbSet<UserRole> UserRoles { get; set; }

        /// <summary>
        /// Gets or sets the refresh tokens in the database.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        /// <summary>
        /// Configures the schema needed for the context with optimized indexes and relationships.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Model mappings and indexes
            // INVESTIGATOR TYPE Configuration
            modelBuilder.Entity<InvestigatorType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_InvestigatorType_Code");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_InvestigatorType_IsActive");
            });

            // INVESTIGATOR INSTANCE Configuration
            modelBuilder.Entity<InvestigatorInstance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomName).HasMaxLength(200);
                entity.HasOne(e => e.Type)
                      .WithMany(t => t.Instances)
                      .HasForeignKey(e => e.TypeId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.TypeId, e.IsActive })
                      .HasDatabaseName("IX_InvestigatorInstance_Type_Active");
                entity.HasIndex(e => e.LastExecutedAt)
                      .HasDatabaseName("IX_InvestigatorInstance_LastExecuted");
            });

            // INVESTIGATION EXECUTION Configuration
            modelBuilder.Entity<InvestigationExecution>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.HasOne(e => e.Investigator)
                      .WithMany(i => i.Executions)
                      .HasForeignKey(e => e.InvestigatorId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.InvestigatorId, e.StartedAt })
                      .HasDatabaseName("IX_Execution_Investigator_Started");
                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_Execution_Status");
            });

            // INVESTIGATION RESULT Configuration
            modelBuilder.Entity<InvestigationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Severity).HasConversion<string>();
                entity.Property(e => e.Message).HasMaxLength(500).IsRequired();
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.HasOne(e => e.Execution)
                      .WithMany(ex => ex.Results)
                      .HasForeignKey(e => e.ExecutionId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Performance Indexes for high-volume queries
                entity.HasIndex(e => new { e.ExecutionId, e.Timestamp })
                      .HasDatabaseName("IX_Result_Execution_Time");
                entity.HasIndex(e => new { e.EntityType, e.EntityId })
                      .HasDatabaseName("IX_Result_Entity");
                entity.HasIndex(e => e.Severity)
                      .HasDatabaseName("IX_Result_Severity");
                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_Result_Timestamp");
            });

            // BUSINESS ENTITY Configurations
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceType).HasConversion<string>();
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalTax).HasPrecision(18, 2);
                entity.Property(e => e.RecipientName).HasMaxLength(200);
                entity.HasIndex(e => e.HasAnomalies).HasDatabaseName("IX_Invoice_Anomalies");
                entity.HasIndex(e => e.IssueDate).HasDatabaseName("IX_Invoice_IssueDate");
                entity.HasIndex(e => e.LastInvestigatedAt).HasDatabaseName("IX_Invoice_LastInvestigated");
            });

            modelBuilder.Entity<Waybill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WaybillType).HasConversion<string>();
                entity.Property(e => e.RecipientName).HasMaxLength(200);
                entity.Property(e => e.ShippedItems).HasMaxLength(1000);
                entity.HasIndex(e => e.HasAnomalies).HasDatabaseName("IX_Waybill_Anomalies");
                entity.HasIndex(e => e.GoodsIssueDate).HasDatabaseName("IX_Waybill_IssueDate");
                entity.HasIndex(e => e.LastInvestigatedAt).HasDatabaseName("IX_Waybill_LastInvestigated");
                entity.HasIndex(e => e.DueDate).HasDatabaseName("IX_Waybill_DueDate");
            });

            // Seed Data for Investigator Types
            modelBuilder.Entity<InvestigatorType>().HasData(
                new InvestigatorType 
                { 
                    Id = 1, 
                    Code = "invoice", 
                    DisplayName = "Invoice Investigator",
                    Description = "Analyzes invoices for anomalies including negative amounts, excessive tax ratios, and future dates",
                    DefaultConfiguration = """{"thresholds":{"maxTaxRatio":0.5,"minAmount":0,"maxFutureDays":0}}""",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new InvestigatorType 
                { 
                    Id = 2, 
                    Code = "waybill", 
                    DisplayName = "Waybill Investigator",
                    Description = "Monitors waybills for delivery delays and identifies shipments older than configured thresholds",
                    DefaultConfiguration = """{"thresholds":{"maxDaysLate":7}}""",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // AUTHENTICATION SYSTEM Configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_User_Username");
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_User_Email");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_User_IsActive");
                entity.HasIndex(e => e.LastLoginAt).HasDatabaseName("IX_User_LastLogin");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Role_Name");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_UserRole_CreatedAt");
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).HasMaxLength(255).IsRequired();
                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Token).IsUnique().HasDatabaseName("IX_RefreshToken_Token");
                entity.HasIndex(e => new { e.UserId, e.IsRevoked })
                      .HasDatabaseName("IX_RefreshToken_User_Revoked");
                entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_RefreshToken_ExpiresAt");
            });

            // Seed Data for Authentication System
            modelBuilder.Entity<Role>().HasData(
                new Role 
                { 
                    Id = 1, 
                    Name = "Admin",
                    Description = "Administrator with full system access",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role 
                { 
                    Id = 2, 
                    Name = "User",
                    Description = "Standard user with limited access",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        /// <summary>
        /// Automatically updates audit fields for entities that support them.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var now = DateTime.UtcNow;

                switch (entry.Entity)
                {
                    case Invoice invoice:
                        if (entry.State == EntityState.Added)
                            invoice.CreatedAt = now;
                        invoice.UpdatedAt = now;
                        break;

                    case Waybill waybill:
                        if (entry.State == EntityState.Added)
                            waybill.CreatedAt = now;
                        waybill.UpdatedAt = now;
                        break;

                    case InvestigatorType type:
                        if (entry.State == EntityState.Added)
                            type.CreatedAt = now;
                        break;

                    case InvestigatorInstance instance:
                        if (entry.State == EntityState.Added)
                            instance.CreatedAt = now;
                        break;

                    case InvestigationExecution execution:
                        if (entry.State == EntityState.Added && execution.StartedAt == default)
                            execution.StartedAt = now;
                        break;

                    case InvestigationResult result:
                        if (entry.State == EntityState.Added && result.Timestamp == default)
                            result.Timestamp = now;
                        break;

                    case User user:
                        if (entry.State == EntityState.Added)
                            user.CreatedAt = now;
                        user.UpdatedAt = now;
                        break;

                    case Role role:
                        if (entry.State == EntityState.Added)
                            role.CreatedAt = now;
                        break;

                    case UserRole userRole:
                        if (entry.State == EntityState.Added)
                            userRole.CreatedAt = now;
                        break;

                    case RefreshToken refreshToken:
                        if (entry.State == EntityState.Added && refreshToken.CreatedAt == default)
                            refreshToken.CreatedAt = now;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
