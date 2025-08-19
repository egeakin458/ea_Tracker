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
        /// Seeds the database with initial admin user and regular user if they don't exist.
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding process");

                // Create admin user if it doesn't exist
                var existingAdmin = await _userService.GetUserByUsernameAsync("admin");
                if (existingAdmin == null)
                {
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
                }
                else
                {
                    _logger.LogInformation("Admin user already exists");
                }

                // Create regular user if it doesn't exist
                var existingUser = await _userService.GetUserByUsernameAsync("user1");
                if (existingUser == null)
                {
                    _logger.LogInformation("Creating initial regular user");
                    var regularUser = await _userService.CreateUserAsync(
                        username: "user1",
                        email: "user1@eatracker.local",
                        password: "User123",
                        displayName: "Regular User"
                    );

                    // Assign user role
                    await _userService.AssignRoleToUserAsync(regularUser.Id, "User");

                    _logger.LogInformation("Successfully created regular user with ID {UserId}", regularUser.Id);
                    _logger.LogInformation("Regular user credentials: username=user1, password=User123");
                }
                else
                {
                    _logger.LogInformation("Regular user already exists");
                }

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