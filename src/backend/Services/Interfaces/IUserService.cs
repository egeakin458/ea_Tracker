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
    }
}