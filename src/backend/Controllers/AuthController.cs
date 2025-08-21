using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services.Authentication;
using ea_Tracker.Services.Interfaces;
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
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtAuthenticationService authService, IUserService userService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// Uses database authentication with secure password validation.
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

                var clientIpAddress = GetClientIpAddress();

                // Check if account is locked
                if (await _userService.IsAccountLockedAsync(request.Username))
                {
                    _logger.LogWarning("Login attempt for locked account {Username} from {RemoteIP}", 
                        request.Username, clientIpAddress);
                    return Unauthorized(new { error = "Account is temporarily locked due to multiple failed attempts" });
                }

                // Validate user credentials against database
                var isValidUser = await _userService.ValidateUserCredentialsAsync(request.Username, request.Password);
                
                if (!isValidUser)
                {
                    // Record failed attempt
                    await _userService.RecordFailedLoginAttemptAsync(request.Username, clientIpAddress);
                    
                    _logger.LogWarning("Failed login attempt for username {Username} from {RemoteIP}", 
                        request.Username, clientIpAddress);
                    
                    // Return generic error to prevent username enumeration
                    return Unauthorized(new { error = "Invalid credentials" });
                }

                // Get user information
                var user = await _userService.GetUserByUsernameAsync(request.Username);
                if (user == null)
                {
                    _logger.LogError("User {Username} validated but not found in database", request.Username);
                    return StatusCode(500, new { error = "An error occurred during authentication" });
                }

                // Get user roles
                var userRoles = await _userService.GetUserRolesAsync(request.Username);
                
                // Generate JWT token with user claims
                var token = _authService.GenerateToken(
                    userId: user.Id.ToString(),
                    username: request.Username,
                    roles: userRoles,
                    additionalClaims: new Dictionary<string, string>
                    {
                        ["login_time"] = DateTimeOffset.UtcNow.ToString(),
                        ["login_ip"] = clientIpAddress
                    }
                );

                var refreshToken = _authService.GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days refresh token validity

                // Store refresh token
                await _userService.StoreRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

                // Record successful login
                await _userService.RecordSuccessfulLoginAsync(request.Username, clientIpAddress);

                _logger.LogInformation("Successful login for user {UserId} ({Username})", user.Id, request.Username);

                var response = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserInfo
                    {
                        Id = user.Id.ToString(),
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
        /// Validates refresh token against database and issues new tokens.
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

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { error = "Invalid refresh token" });
                }

                // Validate refresh token against database
                var user = await _userService.ValidateRefreshTokenAsync(request.RefreshToken);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new { error = "Invalid or expired refresh token" });
                }

                // Get current user roles
                var userRoles = await _userService.GetUserRolesAsync(user.Username);
                
                // Generate new tokens
                var newToken = _authService.GenerateToken(
                    userId: user.Id.ToString(),
                    username: user.Username,
                    roles: userRoles,
                    additionalClaims: new Dictionary<string, string>
                    {
                        ["refresh_time"] = DateTimeOffset.UtcNow.ToString(),
                        ["refresh_ip"] = GetClientIpAddress()
                    }
                );
                
                var newRefreshToken = _authService.GenerateRefreshToken();
                var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Revoke old refresh token
                await _userService.RevokeRefreshTokenAsync(request.RefreshToken);

                // Store new refresh token
                await _userService.StoreRefreshTokenAsync(user.Id, newRefreshToken, newRefreshTokenExpiry);

                _logger.LogInformation("Token refreshed for user {UserId} ({Username})", user.Id, user.Username);

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
        /// Logs out a user by invalidating their refresh tokens.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    // Revoke all refresh tokens for this user
                    await _userService.RevokeAllRefreshTokensAsync(userId);
                    
                    _logger.LogInformation("User {UserId} logged out - all refresh tokens revoked", userId);
                }
                else
                {
                    _logger.LogWarning("Logout request with invalid user ID claim: {UserIdClaim}", userIdClaim);
                }
                
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
        /// Retrieves current user data from the database.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(username) || 
                    !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                // Get current user data from database
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized();
                }

                // Get current roles from database
                var userRoles = await _userService.GetUserRolesAsync(username);

                var profile = new UserProfile
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Roles = userRoles.ToArray(),
                    LastLogin = user.LastLoginAt ?? DateTime.UtcNow
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { error = "An error occurred retrieving profile" });
            }
        }

        /// <summary>
        /// Unlocks a user account (Admin only).
        /// Clears account lockout status and resets failed login attempts.
        /// </summary>
        /// <param name="request">Unlock account request</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("admin/unlock-account")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UnlockAccount([FromBody] UnlockAccountRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var adminUsername = User.Identity?.Name ?? "unknown";
                var clientIpAddress = GetClientIpAddress();

                _logger.LogInformation("Admin {AdminUsername} attempting to unlock account for user {Username} from IP {IpAddress}", 
                    adminUsername, request.Username, clientIpAddress);

                await _userService.UnlockAccountAsync(request.Username);

                _logger.LogInformation("Admin {AdminUsername} successfully unlocked account for user {Username}", 
                    adminUsername, request.Username);

                return Ok(new { message = $"Account for user '{request.Username}' has been unlocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for user {Username}", request.Username);
                return StatusCode(500, new { error = "An error occurred while unlocking the account" });
            }
        }

        /// <summary>
        /// Resets a user's password (Admin only).
        /// Generates a new password hash and clears any account lockout.
        /// </summary>
        /// <param name="request">Reset password request</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("admin/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var adminUsername = User.Identity?.Name ?? "unknown";
                var clientIpAddress = GetClientIpAddress();

                _logger.LogInformation("Admin {AdminUsername} attempting to reset password for user {Username} from IP {IpAddress}", 
                    adminUsername, request.Username, clientIpAddress);

                await _userService.ResetPasswordAsync(request.Username, request.NewPassword);

                _logger.LogInformation("Admin {AdminUsername} successfully reset password for user {Username}", 
                    adminUsername, request.Username);

                return Ok(new { message = $"Password for user '{request.Username}' has been reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Username}", request.Username);
                return StatusCode(500, new { error = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Creates a new user account (Admin only).
        /// Creates a user and assigns the specified role.
        /// </summary>
        /// <param name="request">Create user request</param>
        /// <returns>User creation confirmation</returns>
        [HttpPost("admin/create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CreateUserAsAdminResponse>> CreateUserAsAdmin([FromBody] CreateUserAsAdminRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var adminUsername = User.Identity?.Name ?? "unknown";
                var clientIpAddress = GetClientIpAddress();

                _logger.LogInformation("Admin {AdminUsername} attempting to create user {Username} from IP {IpAddress}", 
                    adminUsername, request.Username, clientIpAddress);

                // Create user using existing service methods
                var user = await _userService.CreateUserAsync(request.Username, request.Email, request.Password, request.DisplayName);
                
                // Assign role using existing service method
                await _userService.AssignRoleToUserAsync(user.Id, request.Role);

                _logger.LogInformation("Admin {AdminUsername} successfully created user {UserId} ({Username}) with role {Role}", 
                    adminUsername, user.Id, user.Username, request.Role);

                // Manual mapping following existing patterns
                var response = new CreateUserAsAdminResponse
                {
                    Message = $"User '{request.Username}' created successfully with role '{request.Role}'",
                    User = new UserInfo
                    {
                        Id = user.Id.ToString(),
                        Username = user.Username,
                        Roles = new[] { request.Role }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username} by admin {AdminUsername}", 
                    request.Username, User.Identity?.Name ?? "unknown");
                return StatusCode(500, new { error = "An error occurred while creating the user" });
            }
        }

        /// <summary>
        /// Gets a paginated list of users with optional filtering.
        /// Administrative endpoint for user management.
        /// </summary>
        /// <param name="request">Query parameters for pagination and filtering</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
        {
            try
            {
                _logger.LogInformation("Admin {AdminUsername} requesting users list (page {Page}, size {PageSize})", 
                    User.Identity?.Name ?? "unknown", request.Page, request.PageSize);

                var (users, totalCount) = await _userService.GetUsersAsync(
                    request.Page, 
                    request.PageSize, 
                    request.Search, 
                    request.RoleFilter);

                var response = new GetUsersResponse
                {
                    Users = users,
                    Pagination = new PaginationInfo
                    {
                        TotalItems = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        HasNextPage = request.Page * request.PageSize < totalCount,
                        HasPreviousPage = request.Page > 1
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list for admin {AdminUsername}", 
                    User.Identity?.Name ?? "unknown");
                return StatusCode(500, new { error = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Toggles the active status of a user (activate/deactivate).
        /// Administrative endpoint for user status management.
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="request">Status update request</param>
        /// <returns>Operation result</returns>
        [HttpPut("admin/users/{userId:int}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserStatus(int userId, [FromBody] ToggleUserStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Admin {AdminUsername} attempting to {Action} user {UserId}", 
                    User.Identity?.Name ?? "unknown", 
                    request.IsActive ? "activate" : "deactivate", 
                    userId);

                var success = await _userService.UpdateUserStatusAsync(userId, request.IsActive, request.Reason);
                
                if (!success)
                {
                    return NotFound(new AdminOperationResponse
                    {
                        Success = false,
                        Message = "User not found or status update failed"
                    });
                }

                var action = request.IsActive ? "activated" : "deactivated";
                var response = new AdminOperationResponse
                {
                    Success = true,
                    Message = $"User has been {action} successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId} by admin {AdminUsername}", 
                    userId, User.Identity?.Name ?? "unknown");
                return StatusCode(500, new AdminOperationResponse
                {
                    Success = false,
                    Message = "An error occurred while updating user status"
                });
            }
        }

        /// <summary>
        /// Updates the role assignment for a user.
        /// Administrative endpoint for role management.
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="request">Role update request</param>
        /// <returns>Operation result</returns>
        [HttpPut("admin/users/{userId:int}/update-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Admin {AdminUsername} attempting to update role for user {UserId} to {NewRole}", 
                    User.Identity?.Name ?? "unknown", userId, request.NewRole);

                // Validate the role exists (basic validation - could be enhanced)
                var validRoles = new[] { "User", "Admin" };
                if (!validRoles.Contains(request.NewRole))
                {
                    return BadRequest(new AdminOperationResponse
                    {
                        Success = false,
                        Message = $"Invalid role. Valid roles are: {string.Join(", ", validRoles)}"
                    });
                }

                var success = await _userService.UpdateUserRoleAsync(userId, request.NewRole, request.Reason);
                
                if (!success)
                {
                    return NotFound(new AdminOperationResponse
                    {
                        Success = false,
                        Message = "User not found or role update failed"
                    });
                }

                var response = new AdminOperationResponse
                {
                    Success = true,
                    Message = $"User role has been updated to {request.NewRole} successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId} to {NewRole} by admin {AdminUsername}", 
                    userId, request.NewRole, User.Identity?.Name ?? "unknown");
                return StatusCode(500, new AdminOperationResponse
                {
                    Success = false,
                    Message = "An error occurred while updating user role"
                });
            }
        }

        /// <summary>
        /// Deletes a user from the system (soft delete).
        /// Administrative endpoint for user removal.
        /// </summary>
        /// <param name="userId">The user ID to delete</param>
        /// <returns>Operation result</returns>
        [HttpDelete("admin/users/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                _logger.LogWarning("Admin {AdminUsername} attempting to delete user {UserId}", 
                    User.Identity?.Name ?? "unknown", userId);

                var success = await _userService.DeleteUserAsync(userId, "Deleted by admin");
                
                if (!success)
                {
                    return NotFound(new AdminOperationResponse
                    {
                        Success = false,
                        Message = "User not found or deletion failed"
                    });
                }

                var response = new AdminOperationResponse
                {
                    Success = true,
                    Message = "User has been deleted successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} by admin {AdminUsername}", 
                    userId, User.Identity?.Name ?? "unknown");
                return StatusCode(500, new AdminOperationResponse
                {
                    Success = false,
                    Message = "An error occurred while deleting user"
                });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific user.
        /// Administrative endpoint for user details.
        /// </summary>
        /// <param name="userId">The user ID to get details for</param>
        /// <returns>Detailed user information</returns>
        [HttpGet("admin/users/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            try
            {
                _logger.LogDebug("Admin {AdminUsername} requesting details for user {UserId}", 
                    User.Identity?.Name ?? "unknown", userId);

                var userDetails = await _userService.GetUserDetailsAsync(userId);
                
                if (userDetails == null)
                {
                    return NotFound(new AdminOperationResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new AdminOperationResponse
                {
                    Success = true,
                    Message = "User details retrieved successfully",
                    Data = userDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving details for user {UserId} by admin {AdminUsername}", 
                    userId, User.Identity?.Name ?? "unknown");
                return StatusCode(500, new AdminOperationResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving user details"
                });
            }
        }

        /// <summary>
        /// Gets system-wide user statistics.
        /// Administrative endpoint for dashboard metrics.
        /// </summary>
        /// <returns>User statistics</returns>
        [HttpGet("admin/users/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                _logger.LogDebug("Admin {AdminUsername} requesting user statistics", 
                    User.Identity?.Name ?? "unknown");

                var stats = await _userService.GetUserStatsAsync();

                return Ok(new AdminOperationResponse
                {
                    Success = true,
                    Message = "User statistics retrieved successfully",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics for admin {AdminUsername}", 
                    User.Identity?.Name ?? "unknown");
                return StatusCode(500, new AdminOperationResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving user statistics"
                });
            }
        }

        #region Private Helper Methods

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

    /// <summary>
    /// Unlock account request model.
    /// </summary>
    public class UnlockAccountRequest
    {
        /// <summary>
        /// The username of the account to unlock.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [SanitizedString(allowHtml: false, maxLength: 100)]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reset password request model.
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// The username of the account to reset password for.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [SanitizedString(allowHtml: false, maxLength: 100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// The new password to set.
        /// </summary>
        [Required]
        [MinLength(6)]
        [MaxLength(200)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Create user as admin request model.
    /// </summary>
    public class CreateUserAsAdminRequest
    {
        [Required]
        [MaxLength(100)]
        [SanitizedString(allowHtml: false, maxLength: 100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;
        
        [MaxLength(200)]
        [SanitizedString(allowHtml: false, maxLength: 200)]
        public string? DisplayName { get; set; }
        
        [Required]
        [MaxLength(50)]
        [SqlSafe]
        public string Role { get; set; } = "User";
    }

    /// <summary>
    /// Create user as admin response model.
    /// </summary>
    public class CreateUserAsAdminResponse
    {
        public string Message { get; set; } = string.Empty;
        public UserInfo User { get; set; } = new();
    }

    /// <summary>
    /// Request to get paginated list of users.
    /// </summary>
    public class GetUsersRequest
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [MaxLength(100)]
        [SanitizedString(allowHtml: false, maxLength: 100)]
        public string? Search { get; set; }

        [MaxLength(50)]
        [SanitizedString(allowHtml: false, maxLength: 50)]
        public string? RoleFilter { get; set; }
    }

    /// <summary>
    /// Response containing paginated users list.
    /// </summary>
    public class GetUsersResponse
    {
        public List<UserSummaryDto> Users { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// User summary information for list display.
    /// </summary>
    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsLocked { get; set; }
    }

    /// <summary>
    /// Pagination information for responses.
    /// </summary>
    public class PaginationInfo
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Request to toggle user status (active/inactive).
    /// </summary>
    public class ToggleUserStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request to update user role.
    /// </summary>
    public class UpdateUserRoleRequest
    {
        [Required]
        [MaxLength(50)]
        [SanitizedString(allowHtml: false, maxLength: 50)]
        public string NewRole { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Standard API response for admin operations.
    /// </summary>
    public class AdminOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Detailed user information for admin view.
    /// </summary>
    public class UserDetailsDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedOutAt { get; set; }
        public bool IsLocked { get; set; }
        public List<UserActivityDto> RecentActivity { get; set; } = new();
    }

    /// <summary>
    /// User activity entry for audit trail.
    /// </summary>
    public class UserActivityDto
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// User statistics for dashboard.
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public int NewUsersThisMonth { get; set; }
        public int LoginAttemptsToday { get; set; }
        public int FailedLoginsToday { get; set; }
    }

    #endregion
}