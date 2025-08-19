using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ea_Tracker.Tests.Services.Implementations
{
    /// <summary>
    /// Comprehensive unit tests for UserService authentication and user management.
    /// Tests password hashing, account lockout, user CRUD operations, and security features.
    /// </summary>
    public class UserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly ILogger<UserService> _logger;

        public UserServiceTests()
        {
            // Each test instance gets a unique database to prevent cross-test contamination
            _context = TestDbContextFactory.CreateInMemoryContext($"{nameof(UserServiceTests)}_{Guid.NewGuid()}");
            _logger = TestLoggerFactory.CreateNullLogger<UserService>();
            _userService = new UserService(_context, _logger);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Password Hashing and Verification Tests

        [Fact]
        public async Task ValidateUserCredentialsAsync_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("testuser", "TestPassword123!");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("testuser", "WrongPassword");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("nonexistent", "password");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_WithInactiveUser_ShouldReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.IsActive = false;
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("testuser", "TestPassword123!");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData(null, "password")]
        [InlineData("username", "")]
        [InlineData("username", null)]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task ValidateUserCredentialsAsync_WithNullOrEmptyCredentials_ShouldReturnFalse(string? username, string? password)
        {
            // Act
            var result = await _userService.ValidateUserCredentialsAsync(username!, password!);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Account Lockout Tests

        [Fact]
        public async Task RecordFailedLoginAttemptAsync_ShouldIncrementFailedAttempts()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            await _userService.RecordFailedLoginAttemptAsync("testuser", "192.168.1.1");

            // Assert
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.FailedLoginAttempts.Should().Be(1);
        }

        [Fact]
        public async Task RecordFailedLoginAttemptAsync_After5Attempts_ShouldLockAccount()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act - Record 5 failed attempts
            for (int i = 0; i < 5; i++)
            {
                await _userService.RecordFailedLoginAttemptAsync("testuser", "192.168.1.1");
            }

            // Assert
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.FailedLoginAttempts.Should().Be(5);
            user.LockedOutAt.Should().NotBeNull();
            user.LockedOutAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task IsAccountLockedAsync_WithLockedAccount_ShouldReturnTrue()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.LockedOutAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 5;
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.IsAccountLockedAsync("testuser");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAccountLockedAsync_WithUnlockedAccount_ShouldReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var result = await _userService.IsAccountLockedAsync("testuser");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsAccountLockedAsync_WithExpiredLockout_ShouldUnlockAccountAndReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.LockedOutAt = DateTime.UtcNow.AddMinutes(-31); // Expired lockout (30 min + 1 min)
            user.FailedLoginAttempts = 5;
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.IsAccountLockedAsync("testuser");

            // Assert
            result.Should().BeFalse();
            
            // Verify account was unlocked
            var updatedUser = await _context.Users.FirstAsync(u => u.Username == "testuser");
            updatedUser.LockedOutAt.Should().BeNull();
            updatedUser.FailedLoginAttempts.Should().Be(0);
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_WithLockedAccount_ShouldReturnFalse()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.LockedOutAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 5;
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("testuser", "TestPassword123!");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RecordSuccessfulLoginAsync_ShouldResetFailedAttemptsAndClearLockout()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _context.Users.FirstAsync(u => u.Username == "testuser");
            user.FailedLoginAttempts = 3;
            user.LockedOutAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Act
            await _userService.RecordSuccessfulLoginAsync("testuser", "192.168.1.1");

            // Assert
            var updatedUser = await _context.Users.FirstAsync(u => u.Username == "testuser");
            updatedUser.FailedLoginAttempts.Should().Be(0);
            updatedUser.LockedOutAt.Should().BeNull();
            updatedUser.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region User CRUD Operations Tests

        [Fact]
        public async Task GetUserByUsernameAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var user = await _userService.GetUserByUsernameAsync("testuser");

            // Assert
            user.Should().NotBeNull();
            user!.Username.Should().Be("testuser");
            user.Email.Should().Be("test@example.com");
            user.UserRoles.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var user = await _userService.GetUserByUsernameAsync("nonexistent");

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithCaseInsensitiveUsername_ShouldReturnUser()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var user = await _userService.GetUserByUsernameAsync("TESTUSER");

            // Assert
            user.Should().NotBeNull();
            user!.Username.Should().Be("testuser");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetUserByUsernameAsync_WithNullOrEmptyUsername_ShouldReturnNull(string? username)
        {
            // Act
            var user = await _userService.GetUserByUsernameAsync(username!);

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var user = await _userService.GetUserByIdAsync(1);

            // Assert
            user.Should().NotBeNull();
            user!.Id.Should().Be(1);
            user.Username.Should().Be("testuser");
            user.UserRoles.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var user = await _userService.GetUserByIdAsync(999);

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithValidData_ShouldCreateUserWithHashedPassword()
        {
            // Arrange
            var username = "newuser";
            var email = "new@example.com";
            var password = "NewPassword123!";
            var displayName = "New User";

            // Act
            var user = await _userService.CreateUserAsync(username, email, password, displayName);

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be(username);
            user.Email.Should().Be(email);
            user.DisplayName.Should().Be(displayName);
            user.IsActive.Should().BeTrue();
            user.PasswordHash.Should().NotBe(password); // Should be hashed
            BCrypt.Net.BCrypt.Verify(password, user.PasswordHash).Should().BeTrue();
            
            // Verify user was saved to database
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingUsername_ShouldThrowException()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act & Assert
            var act = async () => await _userService.CreateUserAsync("testuser", "new@example.com", "password");
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Theory]
        [InlineData("", "email@test.com", "password")]
        [InlineData(null, "email@test.com", "password")]
        [InlineData("username", "", "password")]
        [InlineData("username", null, "password")]
        [InlineData("username", "email@test.com", "")]
        [InlineData("username", "email@test.com", null)]
        public async Task CreateUserAsync_WithInvalidParameters_ShouldThrowException(string? username, string? email, string? password)
        {
            // Act & Assert
            var act = async () => await _userService.CreateUserAsync(username!, email!, password!);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetUserRolesAsync_WithExistingUser_ShouldReturnRoles()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var roles = await _userService.GetUserRolesAsync("testuser");

            // Assert
            roles.Should().NotBeEmpty();
            roles.Should().Contain("User");
        }

        [Fact]
        public async Task GetUserRolesAsync_WithNonExistentUser_ShouldReturnEmptyCollection()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            var roles = await _userService.GetUserRolesAsync("nonexistent");

            // Assert
            roles.Should().BeEmpty();
        }

        #endregion

        #region Role Assignment Tests

        [Fact]
        public async Task AssignRoleToUserAsync_WithValidData_ShouldAssignRole()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act
            await _userService.AssignRoleToUserAsync(1, "Admin");

            // Assert
            var userRoles = await _userService.GetUserRolesAsync("testuser");
            userRoles.Should().Contain("Admin");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WithNonExistentUser_ShouldThrowException()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act & Assert
            var act = async () => await _userService.AssignRoleToUserAsync(999, "Admin");
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WithNonExistentRole_ShouldThrowException()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act & Assert
            var act = async () => await _userService.AssignRoleToUserAsync(1, "NonExistentRole");
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_WithExistingRole_ShouldNotDuplicate()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);

            // Act - Assign the same role twice
            await _userService.AssignRoleToUserAsync(1, "User");
            await _userService.AssignRoleToUserAsync(1, "User");

            // Assert
            var userRoles = await _context.UserRoles.Where(ur => ur.UserId == 1 && ur.Role.Name == "User").ToListAsync();
            userRoles.Should().HaveCount(1); // Should not duplicate
        }

        #endregion

        #region Refresh Token Tests

        [Fact]
        public async Task StoreRefreshTokenAsync_WithValidData_ShouldStoreToken()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var token = "refresh_token_123";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            await _userService.StoreRefreshTokenAsync(1, token, expiresAt);

            // Assert
            var savedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
            savedToken.Should().NotBeNull();
            savedToken!.UserId.Should().Be(1);
            savedToken.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task StoreRefreshTokenAsync_WithInvalidToken_ShouldThrowException(string? token)
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act & Assert
            var act = async () => await _userService.StoreRefreshTokenAsync(1, token!, expiresAt);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnUser()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var token = "refresh_token_123";
            var expiresAt = DateTime.UtcNow.AddDays(7);
            await _userService.StoreRefreshTokenAsync(1, token, expiresAt);

            // Act
            var user = await _userService.ValidateRefreshTokenAsync(token);

            // Assert
            user.Should().NotBeNull();
            user!.Id.Should().Be(1);
            user.UserRoles.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var token = "expired_token";
            var expiredDate = DateTime.UtcNow.AddDays(-1);
            await _userService.StoreRefreshTokenAsync(1, token, expiredDate);

            // Act
            var user = await _userService.ValidateRefreshTokenAsync(token);

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var token = "revoked_token";
            var expiresAt = DateTime.UtcNow.AddDays(7);
            await _userService.StoreRefreshTokenAsync(1, token, expiresAt);
            await _userService.RevokeRefreshTokenAsync(token);

            // Act
            var user = await _userService.ValidateRefreshTokenAsync(token);

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var token = "refresh_token_123";
            var expiresAt = DateTime.UtcNow.AddDays(7);
            await _userService.StoreRefreshTokenAsync(1, token, expiresAt);

            // Act
            await _userService.RevokeRefreshTokenAsync(token);

            // Assert
            var refreshToken = await _context.RefreshTokens.FirstAsync(rt => rt.Token == token);
            refreshToken.IsRevoked.Should().BeTrue();
            refreshToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task RevokeAllRefreshTokensAsync_WithValidUser_ShouldRevokeAllTokens()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var user = await _userService.GetUserByUsernameAsync("testuser");
            user.Should().NotBeNull();
            
            var expiresAt = DateTime.UtcNow.AddDays(7);
            await _userService.StoreRefreshTokenAsync(user!.Id, "token1", expiresAt);
            await _userService.StoreRefreshTokenAsync(user.Id, "token2", expiresAt);
            await _userService.StoreRefreshTokenAsync(user.Id, "token3", expiresAt);

            // Act
            await _userService.RevokeAllRefreshTokensAsync(user.Id);

            // Assert
            var userTokens = await _context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
            userTokens.Should().HaveCount(3);
            userTokens.Should().OnlyContain(rt => rt.IsRevoked);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ValidateUserCredentialsAsync_ShouldCompleteWithinPerformanceThreshold()
        {
            // Arrange
            await TestDbContextFactory.SeedTestDataAsync(_context);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _userService.ValidateUserCredentialsAsync("testuser", "TestPassword123!");
            stopwatch.Stop();

            // Assert
            result.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Credential validation should complete within 2 seconds (adjusted for test environment)");
        }

        [Fact]
        public async Task PasswordHashingPerformance_ShouldCompleteWithinThreshold()
        {
            // Arrange
            var password = "TestPassword123!";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            stopwatch.Stop();

            // Assert
            isValid.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Password hashing and verification should complete within 500ms");
        }

        #endregion
    }
}
