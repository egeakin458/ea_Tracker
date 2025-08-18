using System.Security.Claims;

namespace ea_Tracker.Services.Authentication
{
    /// <summary>
    /// Interface for JWT authentication services providing token generation and validation.
    /// Implements enterprise-grade authentication patterns for secure API access.
    /// </summary>
    public interface IJwtAuthenticationService
    {
        /// <summary>
        /// Generates a JWT token for the specified user with given roles and claims.
        /// </summary>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="username">The username or email</param>
        /// <param name="roles">Collection of user roles</param>
        /// <param name="additionalClaims">Optional additional claims to include</param>
        /// <returns>A JWT token string</returns>
        string GenerateToken(string userId, string username, IEnumerable<string> roles, Dictionary<string, string>? additionalClaims = null);
        
        /// <summary>
        /// Validates a JWT token and extracts claims.
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>Claims principal if valid, null if invalid</returns>
        ClaimsPrincipal? ValidateToken(string token);
        
        /// <summary>
        /// Extracts the user ID from a valid JWT token.
        /// </summary>
        /// <param name="token">The JWT token</param>
        /// <returns>User ID if valid, null if invalid</returns>
        string? GetUserIdFromToken(string token);
        
        /// <summary>
        /// Checks if a token is expired.
        /// </summary>
        /// <param name="token">The JWT token to check</param>
        /// <returns>True if expired, false if valid</returns>
        bool IsTokenExpired(string token);
        
        /// <summary>
        /// Generates a refresh token for extended authentication sessions.
        /// </summary>
        /// <returns>A secure random refresh token</returns>
        string GenerateRefreshToken();
    }
}