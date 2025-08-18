using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services.Authentication;
using ea_Tracker.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// Authentication controller providing JWT token-based authentication endpoints.
    /// Implements secure authentication patterns with input validation and audit logging.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// This is a basic implementation - in production, integrate with your user store.
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Login attempt for username {Username}", request.Username);

                // TODO: In production, validate against your user store (database, Active Directory, etc.)
                // This is a basic implementation for demonstration
                var isValidUser = await ValidateUserCredentials(request.Username, request.Password);
                
                if (!isValidUser)
                {
                    _logger.LogWarning("Failed login attempt for username {Username} from {RemoteIP}", 
                        request.Username, HttpContext.Connection.RemoteIpAddress);
                    
                    // Return generic error to prevent username enumeration
                    return Unauthorized(new { error = "Invalid credentials" });
                }

                // Generate JWT token with user claims
                var userId = await GetUserId(request.Username);
                var userRoles = await GetUserRoles(request.Username);
                
                var token = _authService.GenerateToken(
                    userId: userId,
                    username: request.Username,
                    roles: userRoles,
                    additionalClaims: new Dictionary<string, string>
                    {
                        ["login_time"] = DateTimeOffset.UtcNow.ToString(),
                        ["login_ip"] = GetClientIpAddress()
                    }
                );

                var refreshToken = _authService.GenerateRefreshToken();

                _logger.LogInformation("Successful login for user {UserId} ({Username})", userId, request.Username);

                var response = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserInfo
                    {
                        Id = userId,
                        Username = request.Username,
                        Roles = userRoles.ToArray()
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Match token expiration
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for username {Username}", request.Username);
                return StatusCode(500, new { error = "An error occurred during authentication" });
            }
        }

        /// <summary>
        /// Refreshes an expired JWT token using a refresh token.
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // TODO: In production, validate refresh token against your store
                // For now, we'll implement a basic validation
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { error = "Invalid refresh token" });
                }

                // Extract user information from expired token (validation disabled)
                var principal = _authService.ValidateToken(request.Token);
                if (principal == null)
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { error = "Invalid token claims" });
                }

                // Generate new tokens
                var userRoles = await GetUserRoles(username);
                var newToken = _authService.GenerateToken(
                    userId: userId,
                    username: username,
                    roles: userRoles
                );
                var newRefreshToken = _authService.GenerateRefreshToken();

                _logger.LogInformation("Token refreshed for user {UserId}", userId);

                var response = new RefreshTokenResponse
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { error = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Logs out a user by invalidating their tokens.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public ActionResult Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // TODO: In production, add refresh token to blacklist/revocation list
                
                _logger.LogInformation("User {UserId} logged out", userId);
                
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { error = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Gets the current authenticated user's profile information.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public ActionResult<UserProfile> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
                {
                    return Unauthorized();
                }

                var profile = new UserProfile
                {
                    Id = userId,
                    Username = username,
                    Roles = roles,
                    LastLogin = DateTime.UtcNow // TODO: Get from user store
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { error = "An error occurred retrieving profile" });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates user credentials against the user store.
        /// TODO: Replace with actual user store validation.
        /// </summary>
        private Task<bool> ValidateUserCredentials(string username, string password)
        {
            // TODO: Implement actual password validation with proper hashing
            // This is for demonstration purposes only
            
            // Example basic validation (DO NOT use in production)
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "user", "user123" },
                { "demo", "demo123" }
            };

            var isValid = validUsers.ContainsKey(username.ToLower()) && 
                         validUsers[username.ToLower()] == password;

            return Task.FromResult(isValid);
        }

        /// <summary>
        /// Gets the user ID for a username.
        /// TODO: Replace with actual user store lookup.
        /// </summary>
        private Task<string> GetUserId(string username)
        {
            // Generate a consistent user ID based on username for demo
            var hash = username.GetHashCode();
            var userId = Math.Abs(hash).ToString();
            return Task.FromResult(userId);
        }

        /// <summary>
        /// Gets the roles for a user.
        /// TODO: Replace with actual role lookup from user store.
        /// </summary>
        private Task<IEnumerable<string>> GetUserRoles(string username)
        {
            // Demo role assignment based on username
            var roles = username.ToLower() switch
            {
                "admin" => new[] { "Admin", "User" },
                "user" => new[] { "User" },
                _ => new[] { "User" }
            };

            return Task.FromResult<IEnumerable<string>>(roles);
        }

        /// <summary>
        /// Gets the client IP address with proxy header support.
        /// </summary>
        private string GetClientIpAddress()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        #endregion
    }

    #region DTOs

    /// <summary>
    /// Login request model with input validation.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The username for authentication.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [SanitizedString(allowHtml: false, maxLength: 100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// The password for authentication.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login response model.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// JWT authentication token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for token renewal.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// User information.
        /// </summary>
        public UserInfo User { get; set; } = new();

        /// <summary>
        /// Token expiration time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// User information model.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// User identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User roles.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Refresh token request model.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Expired JWT token.
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Refresh token response model.
    /// </summary>
    public class RefreshTokenResponse
    {
        /// <summary>
        /// New JWT authentication token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// New refresh token.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// User profile model.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// User identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User roles.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Last login time.
        /// </summary>
        public DateTime LastLogin { get; set; }
    }

    #endregion
}