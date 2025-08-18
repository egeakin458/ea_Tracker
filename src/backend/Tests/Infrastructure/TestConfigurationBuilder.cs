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
        /// Creates a configuration builder with test-specific settings.
        /// </summary>
        /// <returns>Configured IConfiguration for testing</returns>
        public static IConfiguration BuildTestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
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
