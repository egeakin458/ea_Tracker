using ea_Tracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Factory for creating test database contexts with in-memory provider.
    /// Ensures test isolation with unique database names per test.
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new test database context with in-memory provider.
        /// Each context gets a unique database name to ensure test isolation.
        /// </summary>
        /// <param name="testName">Name of the test for unique database identification</param>
        /// <returns>Configured ApplicationDbContext for testing</returns>
        public static ApplicationDbContext CreateInMemoryContext(string? testName = null)
        {
            var databaseName = $"TestDatabase_{testName ?? Guid.NewGuid().ToString()}";
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .EnableSensitiveDataLogging() // Helpful for debugging tests
                .Options;

            var context = new ApplicationDbContext(options);
            
            // Ensure database is created
            context.Database.EnsureCreated();
            
            return context;
        }

        /// <summary>
        /// Seeds the test database with common test data.
        /// </summary>
        /// <param name="context">Database context to seed</param>
        public static async Task SeedTestDataAsync(ApplicationDbContext context)
        {
            // Check if test users already exist to prevent duplicate key errors
            if (await context.Users.AnyAsync(u => u.Username == "testuser"))
            {
                return; // Test data already seeded
            }

            // Get or create test roles (they might already exist from seed data)
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                adminRole = new ea_Tracker.Models.Role
                {
                    Name = "Admin",
                    Description = "Administrator role",
                    CreatedAt = DateTime.UtcNow
                };
                context.Roles.Add(adminRole);
            }

            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                userRole = new ea_Tracker.Models.Role
                {
                    Name = "User",
                    Description = "Standard user role",
                    CreatedAt = DateTime.UtcNow
                };
                context.Roles.Add(userRole);
            }

            await context.SaveChangesAsync(); // Save to get IDs

            // Create test users without specifying IDs
            var testUser = new ea_Tracker.Models.User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                DisplayName = "Test User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var adminUser = new ea_Tracker.Models.User
            {
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                DisplayName = "Admin User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(testUser, adminUser);
            await context.SaveChangesAsync(); // Save to get IDs

            // Create user-role assignments using the generated IDs
            var userRoleAssignment = new ea_Tracker.Models.UserRole
            {
                UserId = testUser.Id,
                RoleId = userRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            var adminRoleAssignment = new ea_Tracker.Models.UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.UserRoles.AddRange(userRoleAssignment, adminRoleAssignment);
            await context.SaveChangesAsync();
        }
    }
}
