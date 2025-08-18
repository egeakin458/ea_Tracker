using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a user in the system with authentication and role-based access.
    /// Enhanced with audit fields and security tracking for enterprise use.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique username for authentication.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hashed password for authentication.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the user.
        /// </summary>
        [MaxLength(200)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets when this user record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this user record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the user last logged in.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets the number of failed login attempts.
        /// Used for security monitoring and account lockout policies.
        /// </summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Gets or sets when the account was locked due to failed login attempts.
        /// </summary>
        public DateTime? LockedOutAt { get; set; }

        /// <summary>
        /// Navigation property for user roles.
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Navigation property for refresh tokens.
        /// </summary>
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    /// <summary>
    /// Represents a role in the system for authorization purposes.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Gets or sets the unique identifier for the role.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the role name (e.g., "Admin", "User").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the role.
        /// </summary>
        [MaxLength(200)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets when this role was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Navigation property for user roles.
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    /// <summary>
    /// Junction table for many-to-many relationship between Users and Roles.
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the role identifier.
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// Gets or sets when this user-role assignment was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Navigation property to the user.
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Navigation property to the role.
        /// </summary>
        public virtual Role Role { get; set; } = null!;
    }

    /// <summary>
    /// Represents a refresh token for JWT token renewal.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Gets or sets the unique identifier for the refresh token.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the token value.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier this token belongs to.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets when this token was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this token has been revoked.
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// Gets or sets when this token was revoked.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Navigation property to the user.
        /// </summary>
        public virtual User User { get; set; } = null!;
    }
}