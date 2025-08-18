using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Implementation of user management and authentication services.
    /// Provides secure operations with BCrypt password hashing and comprehensive audit trails.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// Maximum number of failed login attempts before account lockout.
        /// </summary>
        private const int MaxFailedLoginAttempts = 5;

        /// <summary>
        /// Duration of account lockout in minutes.
        /// </summary>
        private const int LockoutDurationMinutes = 30;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                // Check if account is locked
                if (await IsAccountLockedAsync(username))
                {
                    _logger.LogWarning("Login attempt for locked account: {Username}", username);
                    return false;
                }

                // Verify password
                var isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                
                if (!isValidPassword)
                {
                    _logger.LogWarning("Invalid password attempt for user: {Username}", username);
                }

                return isValidPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user: {Username}", username);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            try
            {
                return await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetUserRolesAsync(string username)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                return user?.UserRoles.Select(ur => ur.Role.Name) ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for: {Username}", username);
                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task RecordSuccessfulLoginAsync(string username, string ipAddress)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user != null)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    user.FailedLoginAttempts = 0; // Reset failed attempts on successful login
                    user.LockedOutAt = null; // Clear any lockout
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Recorded successful login for user {Username} from IP {IpAddress}", 
                        username, ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording successful login for user: {Username}", username);
            }
        }

        /// <inheritdoc />
        public async Task RecordFailedLoginAttemptAsync(string username, string ipAddress)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    user.UpdatedAt = DateTime.UtcNow;

                    // Lock account if max attempts reached (exactly at the limit, not beyond)
                    if (user.FailedLoginAttempts == MaxFailedLoginAttempts)
                    {
                        user.LockedOutAt = DateTime.UtcNow;
                        _logger.LogWarning("Account locked due to {AttemptCount} failed login attempts for user {Username} from IP {IpAddress}", 
                            user.FailedLoginAttempts, username, ipAddress);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogWarning("Recorded failed login attempt for user {Username} from IP {IpAddress}", 
                    username, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording failed login attempt for user: {Username}", username);
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsAccountLockedAsync(string username)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user?.LockedOutAt == null)
                {
                    return false;
                }

                // Check if lockout has expired
                var lockoutExpiry = user.LockedOutAt.Value.AddMinutes(LockoutDurationMinutes);
                if (DateTime.UtcNow > lockoutExpiry)
                {
                    // Unlock account
                    user.LockedOutAt = null;
                    user.FailedLoginAttempts = 0;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Account lockout expired and cleared for user: {Username}", username);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking account lockout status for user: {Username}", username);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<User> CreateUserAsync(string username, string email, string password, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Username, email, and password are required");
            }

            try
            {
                // Check if user already exists
                var existingUser = await GetUserByUsernameAsync(username);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"User with username '{username}' already exists");
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    DisplayName = displayName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user: {Username} with email: {Email}", username, email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AssignRoleToUserAsync(int userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role name is required");
            }

            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    throw new InvalidOperationException($"Role '{roleName}' not found");
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        UserId = userId,
                        RoleId = role.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Assigned role {RoleName} to user ID {UserId}", roleName, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user ID {UserId}", roleName, userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StoreRefreshTokenAsync(int userId, string token, DateTime expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token is required");
            }

            try
            {
                var refreshToken = new RefreshToken
                {
                    UserId = userId,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Stored refresh token for user ID {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing refresh token for user ID {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<User?> ValidateRefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(rt => rt.Token == token && 
                                             !rt.IsRevoked && 
                                             rt.ExpiresAt > DateTime.UtcNow);

                return refreshToken?.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task RevokeRefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            try
            {
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (refreshToken != null && !refreshToken.IsRevoked)
                {
                    refreshToken.IsRevoked = true;
                    refreshToken.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Revoked refresh token for user ID {UserId}", refreshToken.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
            }
        }

        /// <inheritdoc />
        public async Task RevokeAllRefreshTokensAsync(int userId)
        {
            try
            {
                var userTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in userTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                if (userTokens.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Revoked {TokenCount} refresh tokens for user ID {UserId}", 
                        userTokens.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all refresh tokens for user ID {UserId}", userId);
            }
        }
    }
}