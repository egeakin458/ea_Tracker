using ea_Tracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service for seeding initial data into the database.
    /// Creates default admin user and ensures system is ready for use.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IUserService _userService;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IUserService userService, ILogger<DatabaseSeeder> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Seeds the database with initial admin user if it doesn't exist.
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding process");

                // Check if admin user already exists
                var existingAdmin = await _userService.GetUserByUsernameAsync("admin");
                if (existingAdmin != null)
                {
                    _logger.LogInformation("Admin user already exists, skipping seed");
                    return;
                }

                // Create admin user
                _logger.LogInformation("Creating initial admin user");
                var adminUser = await _userService.CreateUserAsync(
                    username: "admin",
                    email: "admin@eatracker.local",
                    password: "Admin123",
                    displayName: "System Administrator"
                );

                // Assign admin role
                await _userService.AssignRoleToUserAsync(adminUser.Id, "Admin");
                await _userService.AssignRoleToUserAsync(adminUser.Id, "User");

                _logger.LogInformation("Successfully created admin user with ID {UserId}", adminUser.Id);
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }
    }
}