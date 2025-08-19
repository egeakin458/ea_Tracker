using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ea_Tracker.Configuration;

namespace ea_Tracker.Middleware;

/// <summary>
/// Advanced rate limiting middleware with IP-based, user-based, and endpoint-specific controls.
/// Integrates with existing authentication system and provides security-aware error responses.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;
    private readonly IHostEnvironment _environment;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IMemoryCache cache,
        IOptions<RateLimitingOptions> options,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting if globally disabled
        if (!_options.Global.Enabled)
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Check rate limits in order of specificity
            var rateLimitResult = await CheckRateLimitsAsync(context);

            // Always add rate limit headers (for both success and failure cases)
            AddRateLimitHeaders(context, rateLimitResult);

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded, handling response for {Path}", context.Request.Path);
                await HandleRateLimitExceeded(context, rateLimitResult);
                _logger.LogDebug("Rate limit response handled, status code: {StatusCode}", context.Response.StatusCode);
                return;
            }

            // Continue to next middleware
            await _next(context);

            // Log performance if monitoring is enabled
            if (_options.FeatureFlags.EnablePerformanceMonitoring)
            {
                var duration = DateTime.UtcNow - startTime;
                LogPerformanceMetrics(context, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for {Method} {Path}", context.Request.Method, context.Request.Path);
            
            // Don't continue processing if response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogError("Cannot continue processing - response has already started");
                throw;
            }
            
            await _next(context); // Continue processing on error
        }
    }

    private async Task<RateLimitResult> CheckRateLimitsAsync(HttpContext context)
    {
        var clientIdentifier = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        var userRole = GetUserRole(context);

        // Check endpoint-specific limits first (most restrictive)
        if (_options.FeatureFlags.EnableEndpointRateLimiting && _options.Endpoint.Enabled)
        {
            var endpointResult = await CheckEndpointRateLimit(context, endpoint, clientIdentifier, userRole);
            if (!endpointResult.IsAllowed)
                return endpointResult;
        }

        // Check user-based limits for authenticated users
        if (_options.FeatureFlags.EnableUserRateLimiting && _options.User.Enabled && context.User.Identity?.IsAuthenticated == true)
        {
            var userResult = await CheckUserRateLimit(context, clientIdentifier, userRole);
            if (!userResult.IsAllowed)
                return userResult;
        }

        // Check IP-based limits (fallback for anonymous users)
        if (_options.FeatureFlags.EnableIpRateLimiting && _options.Ip.Enabled)
        {
            return await CheckIpRateLimit(context, clientIdentifier);
        }

        return new RateLimitResult { IsAllowed = true };
    }

    private async Task<RateLimitResult> CheckEndpointRateLimit(HttpContext context, string endpoint, string clientIdentifier, string userRole)
    {
        var applicableRule = _options.Endpoint.Rules.FirstOrDefault(rule => 
            IsEndpointMatch(endpoint, rule.Endpoint) && 
            (rule.ApplicableRoles.Count == 0 || rule.ApplicableRoles.Contains(userRole)));

        if (applicableRule == null)
            return new RateLimitResult { IsAllowed = true };

        var key = applicableRule.PerUser 
            ? $"endpoint_user:{endpoint}:{clientIdentifier}"
            : $"endpoint_global:{endpoint}";

        return await CheckRateLimitInternal(key, applicableRule.RequestsPerMinute, TimeSpan.FromMinutes(1), 
            $"Endpoint rate limit exceeded for {endpoint}");
    }

    private async Task<RateLimitResult> CheckUserRateLimit(HttpContext context, string clientIdentifier, string userRole)
    {
        if (!_options.User.RoleLimits.TryGetValue(userRole, out var roleLimit))
            roleLimit = _options.User.RoleLimits.GetValueOrDefault("User", new UserRoleLimit { RequestsPerMinute = 100 });

        var key = $"user:{clientIdentifier}";
        return await CheckRateLimitInternal(key, roleLimit.RequestsPerMinute, TimeSpan.FromMinutes(1), 
            $"User rate limit exceeded for role {userRole}");
    }

    private async Task<RateLimitResult> CheckIpRateLimit(HttpContext context, string clientIdentifier)
    {
        var ip = GetSafeClientIp(context);
        
        // Check if IP is whitelisted
        if (_options.Ip.WhitelistedIps.Contains(ip))
            return new RateLimitResult { IsAllowed = true };

        var key = $"ip:{ip}";
        
        // Debug logging for rate limiting
        _logger.LogDebug("Checking IP rate limit for {IP} with key {Key}, limit: {Limit}", 
            ip, key, _options.Ip.RequestsPerMinute);
        
        return await CheckRateLimitInternal(key, _options.Ip.RequestsPerMinute, TimeSpan.FromMinutes(1), 
            "IP rate limit exceeded");
    }

    private Task<RateLimitResult> CheckRateLimitInternal(string key, int limit, TimeSpan period, string message)
    {
        var currentTime = DateTime.UtcNow;
        var windowStart = currentTime.Subtract(period);

        // Get or create request log for this key
        var requestLog = _cache.GetOrCreate($"requests:{key}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = period.Add(TimeSpan.FromMinutes(1)); // Keep data a bit longer for headers
            return new List<DateTime>();
        }) ?? new List<DateTime>();

        // Remove expired requests
        requestLog.RemoveAll(time => time < windowStart);

        // Debug logging
        _logger.LogDebug("Rate limit check for key {Key}: current requests={Count}, limit={Limit}, window start={WindowStart}", 
            key, requestLog.Count, limit, windowStart);

        // Check if limit is exceeded
        if (requestLog.Count >= limit)
        {
            var oldestRequest = requestLog.Count > 0 ? requestLog.Min() : currentTime;
            var resetTime = oldestRequest.Add(period);

            _logger.LogWarning("Rate limit EXCEEDED for key {Key}: {Count}/{Limit} requests", key, requestLog.Count, limit);

            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = false,
                Message = message,
                Limit = limit,
                Remaining = 0,
                ResetTime = resetTime,
                RetryAfter = resetTime.Subtract(currentTime)
            });
        }

        // Add current request
        requestLog.Add(currentTime);
        _cache.Set($"requests:{key}", requestLog, period.Add(TimeSpan.FromMinutes(1)));

        var nextResetTime = windowStart.Add(period).Add(TimeSpan.FromMinutes(1));
        _logger.LogDebug("Rate limit OK for key {Key}: {Count}/{Limit} requests", key, requestLog.Count, limit);
        
        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            Limit = limit,
            Remaining = limit - requestLog.Count,
            ResetTime = nextResetTime
        });
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // For authenticated users, use user ID
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                   context.User.FindFirst("sub")?.Value ?? 
                   GetSafeClientIp(context);
        }

        // For anonymous users, use IP address
        return GetSafeClientIp(context);
    }

    private string GetSafeClientIp(HttpContext context)
    {
        try
        {
            if (_options.Ip.EnableRealIpExtraction)
            {
                foreach (var header in _options.Ip.RealIpHeaders)
                {
                    var headerValue = context.Request.Headers[header].FirstOrDefault();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        var firstIp = headerValue.Split(',')[0].Trim();
                        return AnonymizeIp(firstIp);
                    }
                }
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return AnonymizeIp(remoteIp ?? "unknown");
        }
        catch
        {
            return "unknown";
        }
    }

    private static string AnonymizeIp(string ip)
    {
        try
        {
            if (ip.Contains('.')) // IPv4
            {
                var parts = ip.Split('.');
                if (parts.Length == 4)
                    return $"{parts[0]}.{parts[1]}.{parts[2]}.xxx";
            }
            else if (ip.Contains(':')) // IPv6
            {
                var parts = ip.Split(':');
                if (parts.Length >= 6)
                    return string.Join(":", parts.Take(6)) + ":xxxx:xxxx";
            }
            return "anonymized";
        }
        catch
        {
            return "anonymized";
        }
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        return $"{context.Request.Method}:{context.Request.Path}";
    }

    private string GetUserRole(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return "Anonymous";

        return context.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
    }

    private bool IsEndpointMatch(string endpoint, string pattern)
    {
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(endpoint, pattern, StringComparison.OrdinalIgnoreCase);
    }

    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        
        if (result.ResetTime.HasValue)
        {
            var resetTimestamp = ((DateTimeOffset)result.ResetTime.Value).ToUnixTimeSeconds();
            context.Response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString();
        }

        if (!result.IsAllowed && result.RetryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.Value.TotalSeconds).ToString();
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogDebug("Starting to handle rate limit exceeded for {Path}", context.Request.Path);

        // Log rate limit violation with audit information
        if (_options.FeatureFlags.EnableAuditLogging)
        {
            _logger.LogWarning(
                "Rate limit exceeded {CorrelationId} for {Method} {Path} from {ClientIdentifier} (Role: {UserRole})",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                GetClientIdentifier(context),
                GetUserRole(context));
        }

        // Check if response has already started
        if (context.Response.HasStarted)
        {
            _logger.LogError("Cannot set rate limit response - response has already started");
            return;
        }

        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        _logger.LogDebug("Set response status code to 429 for {Path}", context.Request.Path);

        var errorResponse = new
        {
            error = result.Message ?? _options.Global.ExceededMessage,
            correlationId = correlationId,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            retryAfter = result.RetryAfter?.TotalSeconds,
            details = _environment.IsDevelopment() ? new
            {
                limit = result.Limit,
                remaining = result.Remaining,
                resetTime = result.ResetTime?.ToString("yyyy-MM-ddTHH:mm:ssZ")
            } : null
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        _logger.LogDebug("Writing rate limit response body for {Path}", context.Request.Path);
        await context.Response.WriteAsync(jsonResponse);
        _logger.LogDebug("Completed writing rate limit response for {Path}", context.Request.Path);
    }

    private void LogPerformanceMetrics(HttpContext context, TimeSpan duration)
    {
        if (duration.TotalMilliseconds > 5) // Log if rate limiting takes more than 5ms
        {
            _logger.LogWarning(
                "Rate limiting middleware took {Duration}ms for {Method} {Path}",
                duration.TotalMilliseconds,
                context.Request.Method,
                context.Request.Path);
        }
    }
}

/// <summary>
/// Result of rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string? Message { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime? ResetTime { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}