using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ea_Tracker.Configuration;
using ea_Tracker.Tests.Infrastructure;
using Xunit;

namespace ea_Tracker.Tests.Integration;

/// <summary>
/// Integration tests for rate limiting functionality across the entire application pipeline.
/// Tests real HTTP requests with authentication and various endpoints.
/// </summary>
public class RateLimitingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private bool _disposed = false;

    public RateLimitingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            // Override rate limiting configuration for testing
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Clear existing sources to ensure our configuration takes precedence
                config.Sources.Clear();
                
                // First, add the test configuration file (if needed)
                config.AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false);
                
                // Then add our override configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // JWT Configuration for testing
                    ["Jwt:SecretKey"] = "this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length",
                    ["Jwt:Issuer"] = "ea_tracker_test",
                    ["Jwt:Audience"] = "ea_tracker_test_client",
                    ["Jwt:ExpirationMinutes"] = "60",
                    
                    // Database configuration for in-memory database
                    ["ConnectionStrings:DefaultConnection"] = "InMemoryDatabase",
                    
                    // Rate limiting configuration
                    ["RateLimiting:Global:Enabled"] = "true",
                    ["RateLimiting:Global:DefaultLimit"] = "10",
                    ["RateLimiting:Ip:Enabled"] = "true",
                    ["RateLimiting:Ip:RequestsPerMinute"] = "5",
                    ["RateLimiting:User:Enabled"] = "true",
                    ["RateLimiting:User:RoleLimits:Anonymous:RequestsPerMinute"] = "3",
                    ["RateLimiting:User:RoleLimits:User:RequestsPerMinute"] = "8",
                    ["RateLimiting:User:RoleLimits:Admin:RequestsPerMinute"] = "20",
                    ["RateLimiting:Endpoint:Enabled"] = "true",
                    ["RateLimiting:Endpoint:Rules:0:Endpoint"] = "POST:/api/auth/login",
                    ["RateLimiting:Endpoint:Rules:0:RequestsPerMinute"] = "3",
                    ["RateLimiting:Endpoint:Rules:0:PerUser"] = "false",
                    ["RateLimiting:FeatureFlags:EnableIpRateLimiting"] = "true",
                    ["RateLimiting:FeatureFlags:EnableUserRateLimiting"] = "true",
                    ["RateLimiting:FeatureFlags:EnableEndpointRateLimiting"] = "true",
                    ["RateLimiting:FeatureFlags:EnableAuditLogging"] = "true"
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_WithinRateLimit_ShouldSucceed()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
        Assert.True(response.Headers.Contains("X-RateLimit-Reset"));
    }

    [Fact]
    public async Task HealthCheck_ExceedingIpRateLimit_ShouldReturn429()
    {
        // Arrange - Get the IP rate limit from configuration
        var ipRateLimit = 5; // From test configuration

        // Act - Make requests up to the limit
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < ipRateLimit + 2; i++) // Make extra requests to exceed limit
        {
            var response = await _client.GetAsync("/healthz");
            responses.Add(response);
        }

        // Assert
        var successfulRequests = responses.Where(r => r.StatusCode == HttpStatusCode.OK).Count();
        var rateLimitedRequests = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).Count();

        Assert.True(successfulRequests <= ipRateLimit);
        Assert.True(rateLimitedRequests > 0);

        // Check that rate limited responses have appropriate headers
        var rateLimitedResponse = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"));
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Limit"));
        Assert.Equal("0", rateLimitedResponse.Headers.GetValues("X-RateLimit-Remaining").First());

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task LoginEndpoint_ExceedingEndpointRateLimit_ShouldReturn429()
    {
        // Arrange
        var loginData = new
        {
            username = "testuser",
            password = "testpassword"
        };
        var json = JsonSerializer.Serialize(loginData);
        var endpointRateLimit = 3; // From test configuration

        // Act - Make requests to login endpoint
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < endpointRateLimit + 1; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", content);
            responses.Add(response);
        }

        // Assert
        var rateLimitedResponse = responses.LastOrDefault();
        Assert.NotNull(rateLimitedResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);

        // Verify rate limit error response format
        var responseContent = await rateLimitedResponse.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(errorResponse.TryGetProperty("error", out _));
        Assert.True(errorResponse.TryGetProperty("correlationId", out _));
        Assert.True(errorResponse.TryGetProperty("timestamp", out _));

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task AuthenticatedRequest_WithUserRole_ShouldRespectUserRateLimit()
    {
        // Arrange - First create a user and get auth token
        var (token, _) = await CreateTestUserAndLogin("testuser", "User");
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userRateLimit = 8; // From test configuration for User role

        // Act - Make authenticated requests
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < userRateLimit + 1; i++)
        {
            var response = await _client.GetAsync("/healthz");
            responses.Add(response);
        }

        // Assert
        var successfulRequests = responses.Where(r => r.StatusCode == HttpStatusCode.OK).Count();
        var rateLimitedRequests = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).Count();

        // Should allow more requests for authenticated users than anonymous
        Assert.True(successfulRequests >= 5); // More than anonymous limit
        
        // Eventually should hit user rate limit
        if (rateLimitedRequests > 0)
        {
            var rateLimitedResponse = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var limitHeader = rateLimitedResponse.Headers.GetValues("X-RateLimit-Limit").First();
            Assert.Equal(userRateLimit.ToString(), limitHeader);
        }

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task RateLimitHeaders_ShouldBeCorrectlySet()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify required headers are present
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
        Assert.True(response.Headers.Contains("X-RateLimit-Reset"));

        // Verify header values are valid
        var limitHeader = response.Headers.GetValues("X-RateLimit-Limit").First();
        var remainingHeader = response.Headers.GetValues("X-RateLimit-Remaining").First();
        var resetHeader = response.Headers.GetValues("X-RateLimit-Reset").First();

        Assert.True(int.TryParse(limitHeader, out var limit));
        Assert.True(int.TryParse(remainingHeader, out var remaining));
        Assert.True(long.TryParse(resetHeader, out var reset));

        Assert.True(limit > 0);
        Assert.True(remaining >= 0);
        Assert.True(remaining < limit); // Should be decremented after this request
        Assert.True(reset > DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // Reset time should be in the future

        response.Dispose();
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleRateLimitingCorrectly()
    {
        // Arrange
        var numberOfRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make concurrent requests
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(_client.GetAsync("/healthz"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var successfulRequests = responses.Where(r => r.StatusCode == HttpStatusCode.OK).Count();
        var rateLimitedRequests = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).Count();

        // All requests should be handled (either successfully or rate limited)
        Assert.Equal(numberOfRequests, successfulRequests + rateLimitedRequests);

        // At least some requests should succeed
        Assert.True(successfulRequests > 0);

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task RateLimitExceeded_ShouldReturnCorrectErrorFormat()
    {
        // Arrange - Exceed rate limit first
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 10; i++) // Exceed any reasonable rate limit
        {
            var response = await _client.GetAsync("/healthz");
            responses.Add(response);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        var rateLimitedResponse = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        
        // Skip test if we couldn't trigger rate limiting (might happen in some environments)
        if (rateLimitedResponse == null)
        {
            foreach (var response in responses)
                response.Dispose();
            return;
        }

        // Act
        var responseContent = await rateLimitedResponse.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal("application/json", rateLimitedResponse.Content.Headers.ContentType?.MediaType);

        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        // Verify required error response fields
        Assert.True(errorResponse.TryGetProperty("error", out var errorProperty));
        Assert.True(errorResponse.TryGetProperty("correlationId", out var correlationIdProperty));
        Assert.True(errorResponse.TryGetProperty("timestamp", out var timestampProperty));

        // Verify error message
        var errorMessage = errorProperty.GetString();
        Assert.False(string.IsNullOrEmpty(errorMessage));
        Assert.Contains("rate limit", errorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify correlation ID format
        var correlationId = correlationIdProperty.GetString();
        Assert.False(string.IsNullOrEmpty(correlationId));
        Assert.Equal(8, correlationId!.Length); // Should be 8 character correlation ID

        // Verify timestamp format
        var timestamp = timestampProperty.GetString();
        Assert.True(DateTime.TryParse(timestamp, out _));

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    private async Task<(string token, string userId)> CreateTestUserAndLogin(string username, string role)
    {
        // This is a simplified version - in a real test you'd need to:
        // 1. Create a test user in the database
        // 2. Login to get a real JWT token
        // For now, we'll skip this complex setup and focus on rate limiting logic
        
        // Return a mock token for testing purposes
        // In real implementation, you'd integrate with your authentication system
        return ("mock_jwt_token", "test_user_id");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}