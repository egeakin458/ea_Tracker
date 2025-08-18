using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ea_Tracker.Services.Authentication
{
    /// <summary>
    /// JWT authentication service implementation providing secure token generation and validation.
    /// Implements industry-standard JWT practices with configurable security parameters.
    /// </summary>
    public class JwtAuthenticationService : IJwtAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _tokenExpirationMinutes;
        private readonly SymmetricSecurityKey _key;

        public JwtAuthenticationService(IConfiguration configuration, ILogger<JwtAuthenticationService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Retrieve JWT configuration with secure defaults
            _secretKey = configuration["Jwt:SecretKey"] ?? 
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                throw new InvalidOperationException("JWT secret key not configured. Set Jwt:SecretKey in configuration or JWT_SECRET_KEY environment variable.");
            
            _issuer = configuration["Jwt:Issuer"] ?? "ea_tracker_api";
            _audience = configuration["Jwt:Audience"] ?? "ea_tracker_client";
            _tokenExpirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out int expiry) ? expiry : 60;

            // Validate secret key strength
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long for security.");
            }

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        }

        /// <summary>
        /// Generates a JWT token with specified claims and roles.
        /// </summary>
        public string GenerateToken(string userId, string username, IEnumerable<string> roles, Dictionary<string, string>? additionalClaims = null)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId),
                    new(ClaimTypes.Name, username),
                    new(JwtRegisteredClaimNames.Sub, userId),
                    new(JwtRegisteredClaimNames.UniqueName, username),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Add roles as claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Add additional claims if provided
                if (additionalClaims != null)
                {
                    foreach (var claim in additionalClaims)
                    {
                        claims.Add(new Claim(claim.Key, claim.Value));
                    }
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
                    Issuer = _issuer,
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("JWT token generated for user {UserId} with {RoleCount} roles", userId, roles.Count());
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", userId);
                throw new InvalidOperationException("Failed to generate authentication token", ex);
            }
        }

        /// <summary>
        /// Validates a JWT token and returns claims principal.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _key,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                // Ensure token is JWT and uses correct algorithm
                if (validatedToken is JwtSecurityToken jwtToken && 
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }

                _logger.LogWarning("Invalid JWT token format or algorithm");
                return null;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Expired JWT token validation attempt");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Extracts user ID from a JWT token.
        /// </summary>
        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Checks if a JWT token is expired.
        /// </summary>
        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                // If we can't read the token, consider it expired
                return true;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}