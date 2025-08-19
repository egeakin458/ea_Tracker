using Microsoft.Extensions.Configuration;

namespace ea_Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Builder for creating test configurations with appropriate settings.
    /// Provides consistent configuration setup across all test scenarios.
    /// </summary>
    public static class TestConfigurationBuilder
    {
        /// <summary>
        /// Standardized rate limiting configuration constants for consistent testing.
        /// </summary>
        public static class RateLimitingTestConstants
        {
            public const int IP_RATE_LIMIT = 5;
            public const int ANONYMOUS_RATE_LIMIT = 3;
            public const int USER_RATE_LIMIT = 10;
            public const int ADMIN_RATE_LIMIT = 50;
            public const int LOGIN_ENDPOINT_RATE_LIMIT = 3;
        }
        
        /// <summary>
        /// Gets standardized rate limiting configuration dictionary for consistent testing.
        /// </summary>
        public static Dictionary<string, string?> GetStandardRateLimitingConfig()
        {
            return new Dictionary<string, string?>
            {
                ["RateLimiting:Global:Enabled"] = "true",
                ["RateLimiting:Global:DefaultLimit"] = "10",
                ["RateLimiting:Ip:Enabled"] = "true",
                ["RateLimiting:Ip:RequestsPerMinute"] = RateLimitingTestConstants.IP_RATE_LIMIT.ToString(),
                ["RateLimiting:User:Enabled"] = "true",
                ["RateLimiting:User:RoleLimits:Anonymous:RequestsPerMinute"] = RateLimitingTestConstants.ANONYMOUS_RATE_LIMIT.ToString(),
                ["RateLimiting:User:RoleLimits:User:RequestsPerMinute"] = RateLimitingTestConstants.USER_RATE_LIMIT.ToString(),
                ["RateLimiting:User:RoleLimits:Admin:RequestsPerMinute"] = RateLimitingTestConstants.ADMIN_RATE_LIMIT.ToString(),
                ["RateLimiting:Endpoint:Enabled"] = "true",
                ["RateLimiting:Endpoint:Rules:0:Endpoint"] = "POST:/api/auth/login",
                ["RateLimiting:Endpoint:Rules:0:RequestsPerMinute"] = RateLimitingTestConstants.LOGIN_ENDPOINT_RATE_LIMIT.ToString(),
                ["RateLimiting:Endpoint:Rules:0:PerUser"] = "false",
                ["RateLimiting:FeatureFlags:EnableIpRateLimiting"] = "true",
                ["RateLimiting:FeatureFlags:EnableUserRateLimiting"] = "true",
                ["RateLimiting:FeatureFlags:EnableEndpointRateLimiting"] = "true",
                ["RateLimiting:FeatureFlags:EnableAuditLogging"] = "true"
            };
        }
        /// <summary>
        /// Creates a configuration builder with test-specific settings.
        /// Combines JSON configuration with in-memory overrides for reliable testing.
        /// </summary>
        /// <returns>Configured IConfiguration for testing</returns>
        public static IConfiguration BuildTestConfiguration()
        {
            // Start with in-memory defaults to ensure JWT configuration is always available
            var inMemoryConfig = new Dictionary<string, string?>
            {
                // JWT Configuration for testing (essential for startup)
                ["Jwt:SecretKey"] = "this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length",
                ["Jwt:Issuer"] = "ea_tracker_test",
                ["Jwt:Audience"] = "ea_tracker_test_client",
                ["Jwt:ExpirationMinutes"] = "60",
                
                // Database configuration for in-memory database
                ["ConnectionStrings:DefaultConnection"] = "InMemoryDatabase",
                
                // Logging configuration to reduce noise
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            };

            // Add standardized rate limiting configuration
            foreach (var kvp in GetStandardRateLimitingConfig())
            {
                inMemoryConfig[kvp.Key] = kvp.Value;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            return configuration;
        }

        /// <summary>
        /// Creates a configuration with custom JWT settings for specific test scenarios.
        /// </summary>
        /// <param name="secretKey">Custom secret key for JWT</param>
        /// <param name="issuer">Custom issuer</param>
        /// <param name="audience">Custom audience</param>
        /// <param name="expirationMinutes">Custom expiration time</param>
        /// <returns>Configured IConfiguration with custom JWT settings</returns>
        public static IConfiguration BuildCustomJwtConfiguration(
            string? secretKey = null,
            string? issuer = null,
            string? audience = null,
            int? expirationMinutes = null)
        {
            var inMemorySettings = new Dictionary<string, string?>();
            
            // Only add non-null values to test specific scenarios
            if (secretKey != null)
                inMemorySettings["Jwt:SecretKey"] = secretKey;
            else
                inMemorySettings["Jwt:SecretKey"] = "this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length";
                
            if (issuer != null)
                inMemorySettings["Jwt:Issuer"] = issuer;
            else
                inMemorySettings["Jwt:Issuer"] = "ea_tracker_test";
                
            if (audience != null)
                inMemorySettings["Jwt:Audience"] = audience;
            else
                inMemorySettings["Jwt:Audience"] = "ea_tracker_test_client";
                
            if (expirationMinutes != null)
                inMemorySettings["Jwt:ExpirationMinutes"] = expirationMinutes.ToString();
            else
                inMemorySettings["Jwt:ExpirationMinutes"] = "60";

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            return configuration;
        }
        
        /// <summary>
        /// Creates a configuration with completely missing JWT settings for testing error handling.
        /// </summary>
        /// <returns>Configuration without JWT settings</returns>
        public static IConfiguration BuildEmptyConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            return configuration;
        }
    }
}
