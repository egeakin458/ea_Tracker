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

        /// <inheritdoc />
        public async Task UnlockAccountAsync(string username)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to unlock non-existent user: {Username}", username);
                    return;
                }

                user.LockedOutAt = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully unlocked account for user: {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for user: {Username}", username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ResetPasswordAsync(string username, string newPassword)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to reset password for non-existent user: {Username}", username);
                    return;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                
                // Clear any lockout when password is reset
                user.LockedOutAt = null;
                user.FailedLoginAttempts = 0;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully reset password for user: {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {Username}", username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(List<ea_Tracker.Controllers.UserSummaryDto> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string? search = null, string? roleFilter = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(u => u.Username.ToLower().Contains(searchLower) || 
                                           u.Email.ToLower().Contains(searchLower) ||
                                           (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
                }

                // Apply role filter
                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new ea_Tracker.Controllers.UserSummaryDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        DisplayName = u.DisplayName,
                        IsActive = u.IsActive,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToArray(),
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        FailedLoginAttempts = u.FailedLoginAttempts,
                        IsLocked = u.LockedOutAt.HasValue && u.LockedOutAt.Value.AddMinutes(LockoutDurationMinutes) > DateTime.UtcNow
                    })
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} users from {TotalCount} total users (page {Page}, size {PageSize})", 
                    users.Count, totalCount, page, pageSize);

                return (users, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ea_Tracker.Controllers.UserDetailsDto?> GetUserDetailsAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return null;
                }

                var recentActivity = await GetUserActivityAsync(userId, 10);

                var userDetails = new ea_Tracker.Controllers.UserDetailsDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    IsActive = user.IsActive,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray(),
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    LastLoginAt = user.LastLoginAt,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    LockedOutAt = user.LockedOutAt,
                    IsLocked = user.LockedOutAt.HasValue && user.LockedOutAt.Value.AddMinutes(LockoutDurationMinutes) > DateTime.UtcNow,
                    RecentActivity = recentActivity
                };

                _logger.LogDebug("Retrieved detailed information for user {UserId}", userId);

                return userDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive, string? reason = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to update status for non-existent user: {UserId}", userId);
                    return false;
                }

                if (user.IsActive == isActive)
                {
                    _logger.LogDebug("User {UserId} status is already {Status}", userId, isActive ? "active" : "inactive");
                    return true;
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                // Clear lockout when reactivating
                if (isActive)
                {
                    user.LockedOutAt = null;
                    user.FailedLoginAttempts = 0;
                }

                await _context.SaveChangesAsync();

                var action = isActive ? "activated" : "deactivated";
                var auditDetails = !string.IsNullOrWhiteSpace(reason) ? $"Reason: {reason}" : "No reason provided";
                
                _logger.LogInformation("User {Username} ({UserId}) has been {Action}. {Details}", 
                    user.Username, userId, action, auditDetails);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole, string? reason = null)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to update role for non-existent user: {UserId}", userId);
                    return false;
                }

                // Verify the new role exists
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == newRole);
                if (role == null)
                {
                    _logger.LogWarning("Attempted to assign non-existent role {Role} to user {UserId}", newRole, userId);
                    return false;
                }

                // Check if user already has this role
                var hasRole = user.UserRoles.Any(ur => ur.Role.Name == newRole);
                if (hasRole)
                {
                    _logger.LogDebug("User {UserId} already has role {Role}", userId, newRole);
                    return true;
                }

                // Remove all existing roles and add the new one
                var existingRoles = user.UserRoles.ToList();
                foreach (var existingRole in existingRoles)
                {
                    _context.UserRoles.Remove(existingRole);
                }

                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserRoles.Add(userRole);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var oldRoles = string.Join(", ", existingRoles.Select(r => r.Role.Name));
                var auditDetails = !string.IsNullOrWhiteSpace(reason) ? $"Reason: {reason}" : "No reason provided";
                
                _logger.LogInformation("User {Username} ({UserId}) role changed from [{OldRoles}] to [{NewRole}]. {Details}", 
                    user.Username, userId, oldRoles, newRole, auditDetails);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId} to {NewRole}", userId, newRole);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteUserAsync(int userId, string? reason = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent user: {UserId}", userId);
                    return false;
                }

                // Implement soft delete by deactivating the user
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                // Revoke all refresh tokens
                await RevokeAllRefreshTokensAsync(userId);

                await _context.SaveChangesAsync();

                var auditDetails = !string.IsNullOrWhiteSpace(reason) ? $"Reason: {reason}" : "No reason provided";
                
                _logger.LogWarning("User {Username} ({UserId}) has been soft deleted. {Details}", 
                    user.Username, userId, auditDetails);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<ea_Tracker.Controllers.UserStatsDto> GetUserStatsAsync()
        {
            try
            {
                var currentDate = DateTime.UtcNow.Date;
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);

                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = totalUsers - activeUsers;
                var lockedUsers = await _context.Users.CountAsync(u => 
                    u.LockedOutAt.HasValue && u.LockedOutAt.Value.AddMinutes(LockoutDurationMinutes) > DateTime.UtcNow);

                var usersByRole = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.IsActive)
                    .SelectMany(u => u.UserRoles.Select(ur => ur.Role.Name))
                    .GroupBy(role => role)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= monthStart);

                // For simplicity, we'll track login attempts through failed login attempts
                // In a real system, you might want a separate audit table for all login attempts
                var loginAttemptsToday = await _context.Users.SumAsync(u => 
                    u.LastLoginAt.HasValue && u.LastLoginAt.Value.Date == currentDate ? 1 : 0);
                
                var failedLoginsToday = await _context.Users.CountAsync(u => 
                    u.FailedLoginAttempts > 0 && u.UpdatedAt.Date == currentDate);

                var stats = new ea_Tracker.Controllers.UserStatsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    LockedUsers = lockedUsers,
                    UsersByRole = usersByRole,
                    NewUsersThisMonth = newUsersThisMonth,
                    LoginAttemptsToday = loginAttemptsToday,
                    FailedLoginsToday = failedLoginsToday
                };

                _logger.LogDebug("Generated user statistics: {Stats}", System.Text.Json.JsonSerializer.Serialize(stats));

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user statistics");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<ea_Tracker.Controllers.UserActivityDto>> GetUserActivityAsync(int userId, int limit = 20)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new List<ea_Tracker.Controllers.UserActivityDto>();
                }

                // For now, we'll create activity entries based on user data
                // In a production system, you would have a dedicated UserActivity/AuditLog table
                var activities = new List<ea_Tracker.Controllers.UserActivityDto>();

                // Add creation activity
                activities.Add(new ea_Tracker.Controllers.UserActivityDto
                {
                    Timestamp = user.CreatedAt,
                    Action = "User Created",
                    Details = $"User account '{user.Username}' was created",
                    IpAddress = null
                });

                // Add last login activity if available
                if (user.LastLoginAt.HasValue)
                {
                    activities.Add(new ea_Tracker.Controllers.UserActivityDto
                    {
                        Timestamp = user.LastLoginAt.Value,
                        Action = "Login",
                        Details = "User logged in successfully",
                        IpAddress = null
                    });
                }

                // Add lockout activity if user is locked
                if (user.LockedOutAt.HasValue)
                {
                    activities.Add(new ea_Tracker.Controllers.UserActivityDto
                    {
                        Timestamp = user.LockedOutAt.Value,
                        Action = "Account Locked",
                        Details = $"Account locked due to {user.FailedLoginAttempts} failed login attempts",
                        IpAddress = null
                    });
                }

                // Add last update activity
                if (user.UpdatedAt != user.CreatedAt)
                {
                    activities.Add(new ea_Tracker.Controllers.UserActivityDto
                    {
                        Timestamp = user.UpdatedAt,
                        Action = "Profile Updated",
                        Details = "User profile was modified",
                        IpAddress = null
                    });
                }

                // Sort by timestamp descending and take only the requested limit
                var recentActivity = activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

                _logger.LogDebug("Retrieved {Count} activity entries for user {UserId}", recentActivity.Count, userId);

                return recentActivity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user activity for user {UserId}", userId);
                return new List<ea_Tracker.Controllers.UserActivityDto>();
            }
        }
    }
}