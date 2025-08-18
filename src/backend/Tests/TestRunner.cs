using ea_Tracker.Data;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Tests
{
    /// <summary>
    /// Simple test runner for debugging individual authentication scenarios.
    /// </summary>
    public class TestRunner
    {
        public static async Task Main(string[] args)
        {
            await RunDebugTestAsync();
        }
        
        public static async Task RunDebugTestAsync()
        {
            // Create test context
            using var context = TestDbContextFactory.CreateInMemoryContext("DebugTest");
            var logger = TestLoggerFactory.CreateNullLogger<UserService>();
            var userService = new UserService(context, logger);

            // Seed test data
            await TestDbContextFactory.SeedTestDataAsync(context);

            // Debug: Check database state
            var roleCount = await context.Roles.CountAsync();
            var userCount = await context.Users.CountAsync();
            var userRoleCount = await context.UserRoles.CountAsync();
            
            Console.WriteLine($"=== Database State ===");
            Console.WriteLine($"Roles count: {roleCount}");
            Console.WriteLine($"Users count: {userCount}");
            Console.WriteLine($"UserRoles count: {userRoleCount}");
            
            // List all users
            var allUsers = await context.Users.ToListAsync();
            Console.WriteLine($"\n=== Users ===");
            foreach (var u in allUsers)
            {
                Console.WriteLine($"User: {u.Username}, Email: {u.Email}, Active: {u.IsActive}");
            }
            
            // List all roles
            var allRoles = await context.Roles.ToListAsync();
            Console.WriteLine($"\n=== Roles ===");
            foreach (var r in allRoles)
            {
                Console.WriteLine($"Role: {r.Name}, Description: {r.Description}");
            }

            // Debug: Check if user exists
            var user = await userService.GetUserByUsernameAsync("testuser");
            Console.WriteLine($"\n=== User Lookup ===");
            Console.WriteLine($"User found: {user != null}");
            if (user != null)
            {
                Console.WriteLine($"Username: {user.Username}");
                Console.WriteLine($"Email: {user.Email}");
                Console.WriteLine($"IsActive: {user.IsActive}");
                Console.WriteLine($"PasswordHash length: {user.PasswordHash.Length}");
                Console.WriteLine($"UserRoles count: {user.UserRoles.Count}");
                
                // Test manual password verification
                var isValidManual = BCrypt.Net.BCrypt.Verify("TestPassword123!", user.PasswordHash);
                Console.WriteLine($"Manual BCrypt verification: {isValidManual}");
                
                // Check roles
                var roles = await userService.GetUserRolesAsync("testuser");
                Console.WriteLine($"User roles: {string.Join(", ", roles)}");
            }

            // Test the service method
            var result = await userService.ValidateUserCredentialsAsync("testuser", "TestPassword123!");
            Console.WriteLine($"\n=== Service Validation ===");
            Console.WriteLine($"Service validation result: {result}");
            
            // Test with wrong password
            var wrongResult = await userService.ValidateUserCredentialsAsync("testuser", "WrongPassword");
            Console.WriteLine($"Wrong password result: {wrongResult}");
        }
    }
}
