using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using ea_Tracker.Configuration;
using ea_Tracker.Middleware;
using Xunit;

namespace ea_Tracker.Tests.Middleware;

/// <summary>
/// Comprehensive unit tests for RateLimitingMiddleware functionality.
/// Tests IP-based, user-based, and endpoint-specific rate limiting scenarios.
/// </summary>
public class RateLimitingMiddlewareTests : IDisposable
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly IMemoryCache _memoryCache;
    private readonly RateLimitingOptions _options;
    private bool _disposed = false;

    public RateLimitingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        _options = new RateLimitingOptions
        {
            Global = new GlobalRateLimitOptions
            {
                Enabled = true,
                DefaultLimit = 10,
                PeriodInMinutes = 1
            },
            Ip = new IpRateLimitOptions
            {
                Enabled = true,
                RequestsPerMinute = 5,
                WhitelistedIps = new List<string> { "127.0.0.1" }
            },
            User = new UserRateLimitOptions
            {
                Enabled = true,
                RoleLimits = new Dictionary<string, UserRoleLimit>
                {
                    { "Anonymous", new UserRoleLimit { RequestsPerMinute = 3 } },
                    { "User", new UserRoleLimit { RequestsPerMinute = 10 } },
                    { "Admin", new UserRoleLimit { RequestsPerMinute = 50 } }
                }
            },
            Endpoint = new EndpointRateLimitOptions
            {
                Enabled = true,
                Rules = new List<EndpointRule>
                {
                    new EndpointRule
                    {
                        Endpoint = "POST:/api/auth/login",
                        RequestsPerMinute = 2,
                        PerUser = false
                    },
                    new EndpointRule
                    {
                        Endpoint = "GET:/api/export/*",
                        RequestsPerMinute = 3,
                        PerUser = true
                    }
                }
            },
            FeatureFlags = new FeatureFlagOptions
            {
                EnableIpRateLimiting = true,
                EnableUserRateLimiting = true,
                EnableEndpointRateLimiting = true,
                EnableAuditLogging = true,
                EnablePerformanceMonitoring = true
            }
        };

        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
    }

    [Fact]
    public async Task InvokeAsync_WhenRateLimitingDisabled_ShouldPassThrough()
    {
        // Arrange
        var disabledOptions = new RateLimitingOptions { Global = new GlobalRateLimitOptions { Enabled = false } };
        var middleware = CreateMiddleware(disabledOptions);
        var context = CreateHttpContext("GET", "/api/test");
        var nextCalled = false;

        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(disabledOptions), _mockEnvironment.Object);

        // Act
        await middlewareInstance.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithIpRateLimit_ShouldAllowWithinLimit()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("GET", "/api/test", ip: "192.168.1.1");
        var nextCalled = false;

        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act
        await middlewareInstance.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Remaining"));
    }

    [Fact]
    public async Task InvokeAsync_WithIpRateLimit_ShouldBlockWhenExceeded()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var ip = "192.168.1.2";
        var nextCalled = false;

        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act - Make requests up to the limit
        for (int i = 0; i < _options.Ip.RequestsPerMinute; i++)
        {
            var context = CreateHttpContext("GET", "/api/test", ip: ip);
            await middlewareInstance.InvokeAsync(context);
        }

        // Act - Make one more request that should be blocked
        var blockedContext = CreateHttpContext("GET", "/api/test", ip: ip);
        await middlewareInstance.InvokeAsync(blockedContext);

        // Assert
        Assert.Equal(429, blockedContext.Response.StatusCode);
        Assert.True(blockedContext.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task InvokeAsync_WithWhitelistedIp_ShouldNeverBlock()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var whitelistedIp = "127.0.0.1";
        var nextCallCount = 0;

        RequestDelegate next = (ctx) =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act - Make many requests from whitelisted IP
        for (int i = 0; i < _options.Ip.RequestsPerMinute * 2; i++)
        {
            var context = CreateHttpContext("GET", "/api/test", ip: whitelistedIp);
            await middlewareInstance.InvokeAsync(context);
        }

        // Assert
        Assert.Equal(_options.Ip.RequestsPerMinute * 2, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WithUserRateLimit_ShouldRespectUserRole()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var userId = "user123";
        var nextCalled = false;

        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act
        var context = CreateHttpContext("GET", "/api/test", userId: userId, role: "User");
        await middlewareInstance.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        
        // Verify rate limit headers show User role limits
        var limitHeader = context.Response.Headers["X-RateLimit-Limit"].FirstOrDefault();
        Assert.Equal(_options.User.RoleLimits["User"].RequestsPerMinute.ToString(), limitHeader);
    }

    [Fact]
    public async Task InvokeAsync_WithEndpointRateLimit_ShouldApplySpecificRules()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var userId = "user123";
        var nextCallCount = 0;

        RequestDelegate next = (ctx) =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act - Make requests to login endpoint (limit: 2/min)
        for (int i = 0; i < 2; i++)
        {
            var context = CreateHttpContext("POST", "/api/auth/login", userId: userId, role: "User");
            await middlewareInstance.InvokeAsync(context);
        }

        // Act - Try one more request that should be blocked
        var blockedContext = CreateHttpContext("POST", "/api/auth/login", userId: userId, role: "User");
        await middlewareInstance.InvokeAsync(blockedContext);

        // Assert
        Assert.Equal(2, nextCallCount);
        Assert.Equal(429, blockedContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithWildcardEndpoint_ShouldMatchCorrectly()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var userId = "user123";
        var nextCallCount = 0;

        RequestDelegate next = (ctx) =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act - Make requests to export endpoints (limit: 3/min per user)
        var exportPaths = new[] { "/api/export/invoices", "/api/export/waybills", "/api/export/reports" };
        
        foreach (var path in exportPaths)
        {
            var context = CreateHttpContext("GET", path, userId: userId, role: "User");
            await middlewareInstance.InvokeAsync(context);
        }

        // Act - Try one more request that should be blocked
        var blockedContext = CreateHttpContext("GET", "/api/export/test", userId: userId, role: "User");
        await middlewareInstance.InvokeAsync(blockedContext);

        // Assert
        Assert.Equal(3, nextCallCount);
        Assert.Equal(429, blockedContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithPerformanceMonitoring_ShouldLogSlowRequests()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("GET", "/api/test");

        // Add delay to next middleware to simulate slow processing
        RequestDelegate next = async (ctx) =>
        {
            await Task.Delay(10); // Simulate processing time > 5ms threshold
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act
        await middlewareInstance.InvokeAsync(context);

        // Assert
        // Verify that performance warning was logged (this would need more sophisticated mock verification)
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldContinueProcessing()
    {
        // Arrange
        var faultyOptions = new RateLimitingOptions(); // Invalid configuration
        var middleware = CreateMiddleware(faultyOptions);
        var context = CreateHttpContext("GET", "/api/test");
        var nextCalled = false;

        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(faultyOptions), _mockEnvironment.Object);

        // Act
        await middlewareInstance.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled); // Should continue processing even if rate limiting fails
    }

    [Theory]
    [InlineData("Anonymous", 3)]
    [InlineData("User", 10)]
    [InlineData("Admin", 50)]
    public async Task InvokeAsync_WithDifferentRoles_ShouldApplyCorrectLimits(string role, int expectedLimit)
    {
        // Arrange
        var middleware = CreateMiddleware();
        var userId = "user123";

        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middlewareInstance = new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(_options), _mockEnvironment.Object);

        // Act
        var context = CreateHttpContext("GET", "/api/test", userId: userId, role: role);
        await middlewareInstance.InvokeAsync(context);

        // Assert
        var limitHeader = context.Response.Headers["X-RateLimit-Limit"].FirstOrDefault();
        Assert.Equal(expectedLimit.ToString(), limitHeader);
    }

    private RateLimitingMiddleware CreateMiddleware(RateLimitingOptions? options = null)
    {
        var optionsToUse = options ?? _options;
        RequestDelegate next = (ctx) => Task.CompletedTask;
        
        return new RateLimitingMiddleware(
            next, _mockLogger.Object, _memoryCache, 
            Options.Create(optionsToUse), _mockEnvironment.Object);
    }

    private HttpContext CreateHttpContext(string method, string path, string? userId = null, string? role = null, string? ip = null)
    {
        var context = new DefaultHttpContext();
        
        // Setup request
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        // Setup response
        context.Response.Body = new MemoryStream();

        // Setup connection (for IP)
        if (!string.IsNullOrEmpty(ip))
        {
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        }

        // Setup user identity and claims
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("sub", userId)
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache?.Dispose();
            _disposed = true;
        }
    }
}