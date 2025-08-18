using ea_Tracker.Services.Authentication;
using ea_Tracker.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ea_Tracker.Tests.Security
{
    /// <summary>
    /// Security-focused tests for authentication components.
    /// Tests security vulnerabilities, edge cases, and attack vectors.
    /// </summary>
    public class SecurityValidationTests : IDisposable
    {
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly JwtAuthenticationService _jwtService;

        public SecurityValidationTests()
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
        public void BCryptSecurity_PasswordHashing_ShouldBeSecure()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
            var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

            // Assert - Same password should produce different hashes (salt-based)
            hash1.Should().NotBe(hash2);
            hash1.Should().NotBe(password);
            hash2.Should().NotBe(password);
            
            // Assert - Both hashes should verify correctly
            BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
            
            // Assert - Wrong password should not verify
            BCrypt.Net.BCrypt.Verify("WrongPassword", hash1).Should().BeFalse();
        }

        [Fact]
        public void BCryptSecurity_TimingAttackResistance_ShouldBeConsistent()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var wrongPassword = "WrongPassword123!";
            var measurements = new List<long>();

            // Act - Measure verification times for multiple attempts
            for (int i = 0; i < 10; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                BCrypt.Net.BCrypt.Verify(wrongPassword, hash);
                stopwatch.Stop();
                measurements.Add(stopwatch.ElapsedTicks);
            }

            // Assert - Verification should be consistent (no significant timing differences)
            var avgTime = measurements.Average();
            var maxDeviation = measurements.Max() - measurements.Min();
            var deviationPercentage = (double)maxDeviation / avgTime * 100;
            
            deviationPercentage.Should().BeLessThan(200, "BCrypt timing should be reasonably consistent (allowing for test environment variance)");
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("1234567890123456789012345678901")] // 31 characters
        public void JwtSecurity_WeakSecretKeys_ShouldBeRejected(string weakKey)
        {
            // Arrange
            var weakKeyConfig = TestConfigurationBuilder.BuildCustomJwtConfiguration(secretKey: weakKey);

            // Act & Assert
            var act = () => new JwtAuthenticationService(weakKeyConfig, _logger);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void JwtSecurity_TokenSignatureValidation_ShouldDetectTampering()
        {
            // Arrange
            var token = _jwtService.GenerateToken("123", "testuser", new[] { "User" });
            
            // Act - Tamper with the token by changing the last character
            var tamperedToken = token.Substring(0, token.Length - 1) + "X";
            var principal = _jwtService.ValidateToken(tamperedToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void JwtSecurity_TokenAlgorithmValidation_ShouldRejectInvalidAlgorithms()
        {
            // Arrange - Create a token manually with "none" algorithm (security vulnerability)
            var header = new { alg = "none", typ = "JWT" };
            var payload = new { sub = "123", name = "testuser", exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() };
            
            var encodedHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(header)));
            var encodedPayload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)));
            var unsafeToken = $"{encodedHeader}.{encodedPayload}.";

            // Act
            var principal = _jwtService.ValidateToken(unsafeToken);

            // Assert - Should reject tokens with unsafe algorithms
            principal.Should().BeNull();
        }

        [Fact]
        public void JwtSecurity_TokenExpiration_ShouldBeStrictlyEnforced()
        {
            // Arrange
            var shortExpiryConfig = TestConfigurationBuilder.BuildCustomJwtConfiguration(expirationMinutes: 0);
            var shortExpiryService = new JwtAuthenticationService(shortExpiryConfig, _logger);
            
            // Act
            var token = shortExpiryService.GenerateToken("123", "testuser", new[] { "User" });
            
            // Wait to ensure expiration
            Thread.Sleep(1100);
            
            var principal = shortExpiryService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void JwtSecurity_ClaimsInjection_ShouldSanitizeInput()
        {
            // Arrange - Try to inject malicious claims through username
            var maliciousUsername = "testuser\"; \"role\": \"Admin\"; \"";
            var userId = "123";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId, maliciousUsername, roles);
            var principal = _jwtService.ValidateToken(token);

            // Assert - Should not contain injected admin role
            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.Name)?.Value.Should().Be(maliciousUsername); // Should preserve input as-is
            principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().BeEquivalentTo(new[] { "User" }); // Should not contain injected role
        }

        [Fact]
        public void RefreshTokenSecurity_ShouldBeUnpredictable()
        {
            // Arrange & Act - Generate multiple refresh tokens
            var tokens = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                var token = _jwtService.GenerateRefreshToken();
                tokens.Add(token);
            }

            // Assert - All tokens should be unique
            tokens.Count.Should().Be(1000, "All refresh tokens should be unique");
            
            // Assert - Tokens should have sufficient entropy
            foreach (var token in tokens.Take(10))
            {
                var bytes = Convert.FromBase64String(token);
                bytes.Length.Should().Be(64, "Refresh tokens should be 64 bytes long");
                
                // Check for reasonable entropy (no token should be all same byte)
                bytes.Distinct().Count().Should().BeGreaterThan(10, "Token should have reasonable entropy");
            }
        }

        [Theory]
        [InlineData("1")] // Extremely short
        [InlineData("a")] // Single character
        [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890")] // Very long
        public void JwtSecurity_EdgeCaseInputs_ShouldHandleGracefully(string input)
        {
            // Act & Assert - Should not throw exceptions
            var act1 = () => _jwtService.GenerateToken(input, "testuser", new[] { "User" });
            var act2 = () => _jwtService.GenerateToken("123", input, new[] { "User" });
            
            act1.Should().NotThrow();
            act2.Should().NotThrow();
        }

        [Fact]
        public void JwtSecurity_NullByteInjection_ShouldBeHandled()
        {
            // Arrange - Try null byte injection
            var maliciousInput = "testuser\0admin";
            var userId = "123";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId, maliciousInput, roles);
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            var nameClaimValue = principal!.FindFirst(ClaimTypes.Name)?.Value;
            nameClaimValue.Should().NotBeNull();
        }

        [Fact]
        public void JwtSecurity_UnicodeHandling_ShouldWorkCorrectly()
        {
            // Arrange - Test with various Unicode characters
            var unicodeUsername = "用户123🔐Test";
            var userId = "123";
            var roles = new[] { "User" };

            // Act
            var token = _jwtService.GenerateToken(userId, unicodeUsername, roles);
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.Name)?.Value.Should().Be(unicodeUsername);
        }

        [Fact]
        public void JwtSecurity_LargeClaimsPayload_ShouldHandleReasonably()
        {
            // Arrange - Create large additional claims
            var additionalClaims = new Dictionary<string, string>();
            for (int i = 0; i < 100; i++)
            {
                additionalClaims[$"claim_{i}"] = new string('A', 100); // 100 character values
            }

            // Act
            var act = () => _jwtService.GenerateToken("123", "testuser", new[] { "User" }, additionalClaims);
            
            // Assert - Should handle large payloads gracefully
            act.Should().NotThrow();
            
            var token = act.Invoke();
            token.Should().NotBeNullOrWhiteSpace();
            
            // Token should still be validatable
            var principal = _jwtService.ValidateToken(token);
            principal.Should().NotBeNull();
        }

        [Fact]
        public void PasswordSecurity_CommonWeakPasswords_ShouldStillHash()
        {
            // Arrange - Common weak passwords that should still be handled
            var weakPasswords = new[] { "password", "123456", "admin", "", " " };

            foreach (var weakPassword in weakPasswords)
            {
                // Act - BCrypt should still hash even weak passwords
                var act = () => BCrypt.Net.BCrypt.HashPassword(weakPassword);
                
                // Assert
                act.Should().NotThrow();
                
                if (!string.IsNullOrWhiteSpace(weakPassword))
                {
                    var hash = act.Invoke();
                    hash.Should().NotBe(weakPassword);
                    BCrypt.Net.BCrypt.Verify(weakPassword, hash).Should().BeTrue();
                }
            }
        }
    }
}
