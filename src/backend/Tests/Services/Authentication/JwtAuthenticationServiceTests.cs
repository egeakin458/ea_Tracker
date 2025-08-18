using ea_Tracker.Services.Authentication;
using ea_Tracker.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ea_Tracker.Tests.Services.Authentication
{
    /// <summary>
    /// Comprehensive unit tests for JWT authentication service.
    /// Tests token generation, validation, expiration, and security aspects.
    /// </summary>
    public class JwtAuthenticationServiceTests : IDisposable
    {
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly JwtAuthenticationService _jwtService;

        public JwtAuthenticationServiceTests()
        {
            _logger = TestLoggerFactory.CreateNullLogger<JwtAuthenticationService>();
            _configuration = TestConfigurationBuilder.BuildTestConfiguration();
            _jwtService = new JwtAuthenticationService(_configuration, _logger);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            var service = new JwtAuthenticationService(_configuration, _logger);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithShortSecretKey_ShouldThrowException()
        {
            // Arrange
            var shortKeyConfig = TestConfigurationBuilder.BuildCustomJwtConfiguration(secretKey: "short");

            // Act & Assert
            var act = () => new JwtAuthenticationService(shortKeyConfig, _logger);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*at least 32 characters*");
        }

        [Fact]
        public void Constructor_WithMissingSecretKey_ShouldThrowException()
        {
            // Arrange
            var emptyConfig = TestConfigurationBuilder.BuildEmptyConfiguration();

            // Act & Assert
            var act = () => new JwtAuthenticationService(emptyConfig, _logger);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not configured*");
        }

        [Fact]
        public void GenerateToken_WithValidParameters_ShouldReturnValidJwtToken()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User", "Admin" };

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles);

            // Assert
            token.Should().NotBeNullOrWhiteSpace();
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == userId);
            jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == username);
            jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).Should().BeEquivalentTo(roles);
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_ShouldIncludeAllClaims()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User" };
            var additionalClaims = new Dictionary<string, string>
            {
                ["department"] = "IT",
                ["location"] = "HQ"
            };

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles, additionalClaims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Claims.Should().Contain(c => c.Type == "department" && c.Value == "IT");
            jwtToken.Claims.Should().Contain(c => c.Type == "location" && c.Value == "HQ");
        }

        [Fact]
        public void GenerateToken_ShouldIncludeStandardJwtClaims()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        }

        [Fact]
        public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Issuer.Should().Be("ea_tracker_test");
            jwtToken.Audiences.Should().Contain("ea_tracker_test_client");
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User" };
            var token = _jwtService.GenerateToken(userId, username, roles);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
            principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(username);
            principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().Contain(roles);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var principal = _jwtService.ValidateToken(invalidToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange - Create an expired token manually with proper time ordering
            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "123"),
                new(ClaimTypes.Name, "testuser"),
                new(JwtRegisteredClaimNames.Sub, "123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var expiredTokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = now.AddHours(-1),
                Expires = now.AddMinutes(-30), // Expired 30 minutes ago
                IssuedAt = now.AddHours(-1),
                Issuer = "ea_tracker_test",
                Audience = "ea_tracker_test_client",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length")), 
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(expiredTokenDescriptor));

            // Act
            var principal = _jwtService.ValidateToken(expiredToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithDifferentIssuer_ShouldReturnNull()
        {
            // Arrange
            var differentIssuerConfig = TestConfigurationBuilder.BuildCustomJwtConfiguration(issuer: "different_issuer");
            var differentJwtService = new JwtAuthenticationService(differentIssuerConfig, _logger);
            var token = differentJwtService.GenerateToken("123", "testuser", new[] { "User" });

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = "123";
            var token = _jwtService.GenerateToken(userId, "testuser", new[] { "User" });

            // Act
            var extractedUserId = _jwtService.GetUserIdFromToken(token);

            // Assert
            extractedUserId.Should().Be(userId);
        }

        [Fact]
        public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var userId = _jwtService.GetUserIdFromToken(invalidToken);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void IsTokenExpired_WithValidToken_ShouldReturnFalse()
        {
            // Arrange
            var token = _jwtService.GenerateToken("123", "testuser", new[] { "User" });

            // Act
            var isExpired = _jwtService.IsTokenExpired(token);

            // Assert
            isExpired.Should().BeFalse();
        }

        [Fact]
        public void IsTokenExpired_WithExpiredToken_ShouldReturnTrue()
        {
            // Arrange - Create an expired token manually with proper time ordering
            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "123"),
                new(ClaimTypes.Name, "testuser"),
                new(JwtRegisteredClaimNames.Sub, "123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var expiredTokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = now.AddHours(-1),
                Expires = now.AddMinutes(-30), // Expired 30 minutes ago
                IssuedAt = now.AddHours(-1),
                Issuer = "ea_tracker_test",
                Audience = "ea_tracker_test_client",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length")), 
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(expiredTokenDescriptor));

            // Act
            var isExpired = _jwtService.IsTokenExpired(expiredToken);

            // Assert
            isExpired.Should().BeTrue();
        }

        [Fact]
        public void IsTokenExpired_WithInvalidToken_ShouldReturnTrue()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var isExpired = _jwtService.IsTokenExpired(invalidToken);

            // Assert
            isExpired.Should().BeTrue();
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnUniqueTokens()
        {
            // Act
            var token1 = _jwtService.GenerateRefreshToken();
            var token2 = _jwtService.GenerateRefreshToken();

            // Assert
            token1.Should().NotBeNullOrWhiteSpace();
            token2.Should().NotBeNullOrWhiteSpace();
            token1.Should().NotBe(token2);
            
            // Verify it's base64 encoded
            var bytes1 = Convert.FromBase64String(token1);
            var bytes2 = Convert.FromBase64String(token2);
            bytes1.Length.Should().Be(64);
            bytes2.Length.Should().Be(64);
        }

        [Fact]
        public void GenerateToken_WithEmptyRoles_ShouldGenerateTokenWithoutRoleClaims()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = Array.Empty<string>();

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GenerateToken_WithInvalidUserId_ShouldStillGenerateToken(string? userId)
        {
            // Arrange
            var username = "testuser";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId ?? "", username, roles);

            // Assert
            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TokenGeneration_ShouldCompleteWithinPerformanceThreshold()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var roles = new[] { "User", "Admin" };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var token = _jwtService.GenerateToken(userId, username, roles);
            stopwatch.Stop();

            // Assert
            token.Should().NotBeNullOrWhiteSpace();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Token generation should complete within 200ms");
        }

        [Fact]
        public void TokenValidation_ShouldCompleteWithinPerformanceThreshold()
        {
            // Arrange
            var token = _jwtService.GenerateToken("123", "testuser", new[] { "User" });
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var principal = _jwtService.ValidateToken(token);
            stopwatch.Stop();

            // Assert
            principal.Should().NotBeNull();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Token validation should complete within 200ms");
        }
    }
}
