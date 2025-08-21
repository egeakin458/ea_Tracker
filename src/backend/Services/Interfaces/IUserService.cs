using ea_Tracker.Models;

namespace ea_Tracker.Services.Interfaces
{
    /// <summary>
    /// Interface for user management and authentication services.
    /// Provides secure user operations with comprehensive audit trails.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Validates user credentials against the database.
        /// Implements secure password verification with BCrypt.
        /// </summary>
        /// <param name="username">The username to validate</param>
        /// <param name="password">The plain text password to verify</param>
        /// <returns>True if credentials are valid; otherwise, false</returns>
        Task<bool> ValidateUserCredentialsAsync(string username, string password);

        /// <summary>
        /// Gets user information by username for authentication.
        /// </summary>
        /// <param name="username">The username to look up</param>
        /// <returns>User entity if found; otherwise, null</returns>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Gets user information by ID.
        /// </summary>
        /// <param name="userId">The user ID to look up</param>
        /// <returns>User entity if found; otherwise, null</returns>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Gets the roles assigned to a user.
        /// </summary>
        /// <param name="username">The username to get roles for</param>
        /// <returns>Collection of role names</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(string username);

        /// <summary>
        /// Records a successful login for audit and security tracking.
        /// </summary>
        /// <param name="username">The username that logged in</param>
        /// <param name="ipAddress">The IP address of the login attempt</param>
        Task RecordSuccessfulLoginAsync(string username, string ipAddress);

        /// <summary>
        /// Records a failed login attempt for security monitoring.
        /// </summary>
        /// <param name="username">The username that attempted to log in</param>
        /// <param name="ipAddress">The IP address of the failed attempt</param>
        Task RecordFailedLoginAttemptAsync(string username, string ipAddress);

        /// <summary>
        /// Checks if a user account is locked due to failed login attempts.
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if account is locked; otherwise, false</returns>
        Task<bool> IsAccountLockedAsync(string username);

        /// <summary>
        /// Creates a new user in the system.
        /// Hashes the password securely before storage.
        /// </summary>
        /// <param name="username">The username for the new user</param>
        /// <param name="email">The email address for the new user</param>
        /// <param name="password">The plain text password to hash and store</param>
        /// <param name="displayName">Optional display name</param>
        /// <returns>The created user entity</returns>
        Task<User> CreateUserAsync(string username, string email, string password, string? displayName = null);

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="roleName">The role name to assign</param>
        Task AssignRoleToUserAsync(int userId, string roleName);

        /// <summary>
        /// Stores a refresh token for a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The refresh token value</param>
        /// <param name="expiresAt">When the token expires</param>
        Task StoreRefreshTokenAsync(int userId, string token, DateTime expiresAt);

        /// <summary>
        /// Validates a refresh token and returns the associated user.
        /// </summary>
        /// <param name="token">The refresh token to validate</param>
        /// <returns>User if token is valid; otherwise, null</returns>
        Task<User?> ValidateRefreshTokenAsync(string token);

        /// <summary>
        /// Revokes a refresh token.
        /// </summary>
        /// <param name="token">The refresh token to revoke</param>
        Task RevokeRefreshTokenAsync(string token);

        /// <summary>
        /// Revokes all refresh tokens for a user (useful for logout all devices).
        /// </summary>
        /// <param name="userId">The user ID</param>
        Task RevokeAllRefreshTokensAsync(int userId);

        /// <summary>
        /// Unlocks a user account by clearing lockout status and resetting failed attempts.
        /// Administrative function requiring proper authorization.
        /// </summary>
        /// <param name="username">The username to unlock</param>
        Task UnlockAccountAsync(string username);

        /// <summary>
        /// Resets a user's password to a new value.
        /// Administrative function requiring proper authorization.
        /// </summary>
        /// <param name="username">The username to reset password for</param>
        /// <param name="newPassword">The new password (will be hashed)</param>
        Task ResetPasswordAsync(string username, string newPassword);

        /// <summary>
        /// Gets a paginated list of users with optional search and role filtering.
        /// Administrative function with comprehensive filtering support.
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="search">Optional search term for username or email</param>
        /// <param name="roleFilter">Optional role filter</param>
        /// <returns>Paginated user list with summary information</returns>
        Task<(List<ea_Tracker.Controllers.UserSummaryDto> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string? search = null, string? roleFilter = null);

        /// <summary>
        /// Gets detailed information about a specific user.
        /// Administrative function returning comprehensive user data.
        /// </summary>
        /// <param name="userId">The user ID to retrieve details for</param>
        /// <returns>Detailed user information including audit trail</returns>
        Task<ea_Tracker.Controllers.UserDetailsDto?> GetUserDetailsAsync(int userId);

        /// <summary>
        /// Updates the active status of a user (activate/deactivate).
        /// Administrative function with audit logging.
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="isActive">New active status</param>
        /// <param name="reason">Optional reason for the change</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive, string? reason = null);

        /// <summary>
        /// Updates the role assignment for a user.
        /// Administrative function with validation and audit logging.
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="newRole">The new role to assign</param>
        /// <param name="reason">Optional reason for the change</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateUserRoleAsync(int userId, string newRole, string? reason = null);

        /// <summary>
        /// Soft deletes a user from the system.
        /// Administrative function implementing soft delete pattern.
        /// </summary>
        /// <param name="userId">The user ID to delete</param>
        /// <param name="reason">Optional reason for deletion</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteUserAsync(int userId, string? reason = null);

        /// <summary>
        /// Gets system-wide user statistics for dashboard display.
        /// Administrative function providing user metrics.
        /// </summary>
        /// <returns>User statistics including counts by role and activity</returns>
        Task<ea_Tracker.Controllers.UserStatsDto> GetUserStatsAsync();

        /// <summary>
        /// Gets recent activity for a specific user.
        /// Administrative function providing user audit trail.
        /// </summary>
        /// <param name="userId">The user ID to get activity for</param>
        /// <param name="limit">Maximum number of activity entries to return</param>
        /// <returns>List of user activity entries</returns>
        Task<List<ea_Tracker.Controllers.UserActivityDto>> GetUserActivityAsync(int userId, int limit = 20);
    }
}