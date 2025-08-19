using ea_Tracker.Data;
using ea_Tracker.Services.Authentication;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ea_Tracker.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete authentication flow.
    /// Tests the interaction between JWT service and User service components.
    /// </summary>
    public class AuthenticationIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly JwtAuthenticationService _jwtService;
        private readonly ILogger<UserService> _userLogger;
        private readonly ILogger<JwtAuthenticationService> _jwtLogger;
        private readonly IConfiguration _configuration;

        public AuthenticationIntegrationTests()
        {
            // Each test instance gets a unique database to prevent cross-test contamination
            _context = TestDbContextFactory.CreateInMemoryContext($"{nameof(AuthenticationIntegrationTests)}_{Guid.NewGuid()}");
            _userLogger = TestLoggerFactory.CreateNullLogger<UserService>();
            _jwtLogger = TestLoggerFactory.CreateNullLogger<JwtAuthenticationService>();
            _configuration = TestConfigurationBuilder.BuildTestConfiguration();
            
            _userService = new UserService(_context, _userLogger);
            _jwtService = new JwtAuthenticationService(_configuration, _jwtLogger);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task CompleteAuthenticationFlow_WithValidCredentials_ShouldGenerateValidTokens()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var password = "TestPassword123!";
            var ipAddress = "192.168.1.1";

            // Act - Step 1: Validate credentials
            var isValidCredentials = await _userService.ValidateUserCredentialsAsync(username, password);
            
            // Act - Step 2: Get user details for token generation
            var user = await _userService.GetUserByUsernameAsync(username);
            var userRoles = await _userService.GetUserRolesAsync(username);
            
            // Act - Step 3: Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user!.Id.ToString(), username, userRoles);
            
            // Act - Step 4: Generate and store refresh token
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _userService.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));
            
            // Act - Step 5: Record successful login
            await _userService.RecordSuccessfulLoginAsync(username, ipAddress);

            // Assert - All steps should complete successfully
            isValidCredentials.Should().BeTrue();
            user.Should().NotBeNull();
            userRoles.Should().NotBeEmpty();
            jwtToken.Should().NotBeNullOrWhiteSpace();
            refreshToken.Should().NotBeNullOrWhiteSpace();
            
            // Assert - JWT token should be valid and contain correct claims
            var principal = _jwtService.ValidateToken(jwtToken);
            principal.Should().NotBeNull();
            principal!.Identity!.Name.Should().Be(username);
            
            // Assert - User login should be recorded
            var updatedUser = await _userService.GetUserByIdAsync(user.Id);
            updatedUser!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            updatedUser.FailedLoginAttempts.Should().Be(0);
        }

        [Fact]
        public async Task CompleteAuthenticationFlow_WithInvalidCredentials_ShouldHandleFailedAttempts()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var wrongPassword = "WrongPassword123!";
            var ipAddress = "192.168.1.1";

            // Act - Step 1: Attempt authentication with wrong password
            var isValidCredentials = await _userService.ValidateUserCredentialsAsync(username, wrongPassword);
            
            // Act - Step 2: Record failed attempt only if validation failed (matching production pattern)
            if (!isValidCredentials)
            {
                await _userService.RecordFailedLoginAttemptAsync(username, ipAddress);
            }

            // Assert - Authentication should fail
            isValidCredentials.Should().BeFalse();
            
            // Assert - Failed attempt should be recorded
            var user = await _userService.GetUserByUsernameAsync(username);
            user!.FailedLoginAttempts.Should().Be(1);
        }

        [Fact]
        public async Task RefreshTokenFlow_WithValidToken_ShouldGenerateNewJwtToken()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _userService.GetUserByIdAsync(1);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _userService.StoreRefreshTokenAsync(user!.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            // Act - Step 1: Validate refresh token
            var validatedUser = await _userService.ValidateRefreshTokenAsync(refreshToken);
            
            // Act - Step 2: Generate new JWT token
            var userRoles = await _userService.GetUserRolesAsync(validatedUser!.Username);
            var newJwtToken = _jwtService.GenerateToken(validatedUser.Id.ToString(), validatedUser.Username, userRoles);
            
            // Act - Step 3: Generate new refresh token
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            
            // Act - Step 4: Revoke old refresh token and store new one
            await _userService.RevokeRefreshTokenAsync(refreshToken);
            await _userService.StoreRefreshTokenAsync(validatedUser.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));

            // Assert
            validatedUser.Should().NotBeNull();
            newJwtToken.Should().NotBeNullOrWhiteSpace();
            newRefreshToken.Should().NotBeNullOrWhiteSpace();
            newRefreshToken.Should().NotBe(refreshToken);
            
            // Assert - New JWT token should be valid
            var principal = _jwtService.ValidateToken(newJwtToken);
            principal.Should().NotBeNull();
            principal!.Identity!.Name.Should().Be(validatedUser.Username);
            
            // Assert - Old refresh token should be revoked
            var revokedUser = await _userService.ValidateRefreshTokenAsync(refreshToken);
            revokedUser.Should().BeNull();
        }

        [Fact]
        public async Task AccountLockoutFlow_After5FailedAttempts_ShouldPreventAuthentication()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var correctPassword = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var ipAddress = "192.168.1.1";

            // Act - Step 1: Record 5 failed attempts (matching production AuthController.Login() flow)
            for (int i = 0; i < 5; i++)
            {
                var isValid = await _userService.ValidateUserCredentialsAsync(username, wrongPassword);
                isValid.Should().BeFalse(); // Ensure validation fails
                
                // Only record failed attempt if validation failed (matching production pattern)
                if (!isValid)
                {
                    await _userService.RecordFailedLoginAttemptAsync(username, ipAddress);
                }
            }

            // Act - Step 2: Try to authenticate with correct password (should fail due to lockout)
            var isValidAfterLockout = await _userService.ValidateUserCredentialsAsync(username, correctPassword);

            // Assert - Account should be locked
            var isLocked = await _userService.IsAccountLockedAsync(username);
            isLocked.Should().BeTrue();
            isValidAfterLockout.Should().BeFalse();
            
            // Assert - User should have 5 failed attempts and lockout timestamp
            var user = await _userService.GetUserByUsernameAsync(username);
            user!.FailedLoginAttempts.Should().Be(5);
            user.LockedOutAt.Should().NotBeNull();
        }

        [Fact]
        public async Task AccountLockoutRecovery_AfterExpiry_ShouldAllowAuthentication()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var correctPassword = "TestPassword123!";
            
            // Manually set expired lockout (31 minutes ago)
            var user = await _userService.GetUserByUsernameAsync(username);
            user!.LockedOutAt = DateTime.UtcNow.AddMinutes(-31);
            user.FailedLoginAttempts = 5;
            await _context.SaveChangesAsync();

            // Act - Check if account is still locked (should auto-unlock)
            var isLocked = await _userService.IsAccountLockedAsync(username);
            
            // Act - Try to authenticate
            var isValid = await _userService.ValidateUserCredentialsAsync(username, correctPassword);

            // Assert - Account should be unlocked and authentication should succeed
            isLocked.Should().BeFalse();
            isValid.Should().BeTrue();
            
            // Assert - Failed attempts should be reset
            var updatedUser = await _userService.GetUserByUsernameAsync(username);
            updatedUser!.FailedLoginAttempts.Should().Be(0);
            updatedUser.LockedOutAt.Should().BeNull();
        }

        [Fact]
        public async Task UserRegistrationFlow_WithRoleAssignment_ShouldCreateCompleteUserProfile()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "newuser";
            var email = "newuser@example.com";
            var password = "NewUserPassword123!";
            var displayName = "New User";
            var roleName = "User";

            // Act - Step 1: Create user
            var createdUser = await _userService.CreateUserAsync(username, email, password, displayName);
            
            // Act - Step 2: Assign role
            await _userService.AssignRoleToUserAsync(createdUser.Id, roleName);
            
            // Act - Step 3: Verify user can authenticate
            var isValid = await _userService.ValidateUserCredentialsAsync(username, password);
            
            // Act - Step 4: Generate token for new user
            var userRoles = await _userService.GetUserRolesAsync(username);
            var jwtToken = _jwtService.GenerateToken(createdUser.Id.ToString(), username, userRoles);

            // Assert - User creation and setup should be complete
            createdUser.Should().NotBeNull();
            createdUser.Username.Should().Be(username);
            createdUser.Email.Should().Be(email);
            createdUser.DisplayName.Should().Be(displayName);
            
            // Assert - Authentication should work
            isValid.Should().BeTrue();
            userRoles.Should().Contain(roleName);
            jwtToken.Should().NotBeNullOrWhiteSpace();
            
            // Assert - JWT token should be valid
            var principal = _jwtService.ValidateToken(jwtToken);
            principal.Should().NotBeNull();
            principal!.Identity!.Name.Should().Be(username);
        }

        [Fact]
        public async Task PerformanceTest_CompleteAuthenticationFlow_ShouldMeetThresholds()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var password = "TestPassword123!";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Complete authentication flow
            var isValid = await _userService.ValidateUserCredentialsAsync(username, password);
            var user = await _userService.GetUserByUsernameAsync(username);
            var roles = await _userService.GetUserRolesAsync(username);
            var jwtToken = _jwtService.GenerateToken(user!.Id.ToString(), username, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _userService.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));
            await _userService.RecordSuccessfulLoginAsync(username, "127.0.0.1");
            
            stopwatch.Stop();

            // Assert - Performance requirements
            isValid.Should().BeTrue();
            jwtToken.Should().NotBeNullOrWhiteSpace();
            refreshToken.Should().NotBeNullOrWhiteSpace();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Complete authentication flow should complete within 3 seconds (adjusted for test environment)");
        }

        [Fact]
        public async Task ConcurrencyTest_MultipleFailedAttempts_ShouldHandleCorrectly()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var username = "testuser";
            var wrongPassword = "WrongPassword123!";
            var ipAddress = "192.168.1.1";

            // Act - Simulate concurrent failed login attempts (matching production AuthController.Login() flow)
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var isValid = await _userService.ValidateUserCredentialsAsync(username, wrongPassword);
                    // Only record failed attempt if validation failed (matching production pattern)
                    if (!isValid)
                    {
                        await _userService.RecordFailedLoginAttemptAsync(username, ipAddress);
                    }
                }));
            }
            
            await Task.WhenAll(tasks);

            // Assert - Account should be locked after reaching threshold
            var user = await _userService.GetUserByUsernameAsync(username);
            var isLocked = await _userService.IsAccountLockedAsync(username);
            
            user!.FailedLoginAttempts.Should().BeGreaterOrEqualTo(5);
            isLocked.Should().BeTrue();
        }
    }
}
