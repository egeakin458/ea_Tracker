namespace ea_Tracker.Configuration;

/// <summary>
/// Configuration options for API rate limiting functionality.
/// Provides fine-grained control over rate limiting policies across different user types and endpoints.
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Global rate limiting settings that apply to all requests
    /// </summary>
    public GlobalRateLimitOptions Global { get; set; } = new();

    /// <summary>
    /// IP-based rate limiting (anonymous requests)
    /// </summary>
    public IpRateLimitOptions Ip { get; set; } = new();

    /// <summary>
    /// User-based rate limiting (authenticated requests)
    /// </summary>
    public UserRateLimitOptions User { get; set; } = new();

    /// <summary>
    /// Endpoint-specific rate limiting rules
    /// </summary>
    public EndpointRateLimitOptions Endpoint { get; set; } = new();

    /// <summary>
    /// Feature flags for gradual rollout
    /// </summary>
    public FeatureFlagOptions FeatureFlags { get; set; } = new();
}

/// <summary>
/// Global rate limiting configuration
/// </summary>
public class GlobalRateLimitOptions
{
    /// <summary>
    /// Enable or disable rate limiting globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per time period for any single client
    /// </summary>
    public int DefaultLimit { get; set; } = 100;

    /// <summary>
    /// Time period in minutes for the default limit
    /// </summary>
    public int PeriodInMinutes { get; set; } = 1;

    /// <summary>
    /// Message to return when rate limit is exceeded
    /// </summary>
    public string ExceededMessage { get; set; } = "API rate limit exceeded. Please try again later.";
}

/// <summary>
/// IP-based rate limiting for anonymous requests
/// </summary>
public class IpRateLimitOptions
{
    /// <summary>
    /// Enable IP-based rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute for anonymous users
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Whitelist of IPs to exempt from rate limiting
    /// </summary>
    public List<string> WhitelistedIps { get; set; } = new();

    /// <summary>
    /// Enable real IP extraction from proxy headers
    /// </summary>
    public bool EnableRealIpExtraction { get; set; } = true;

    /// <summary>
    /// Headers to check for real IP (in order of preference)
    /// </summary>
    public List<string> RealIpHeaders { get; set; } = new() { "X-Forwarded-For", "X-Real-IP" };
}

/// <summary>
/// User-based rate limiting for authenticated requests
/// </summary>
public class UserRateLimitOptions
{
    /// <summary>
    /// Enable user-based rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Rate limits per user role
    /// </summary>
    public Dictionary<string, UserRoleLimit> RoleLimits { get; set; } = new()
    {
        { "Anonymous", new UserRoleLimit { RequestsPerMinute = 60 } },
        { "User", new UserRoleLimit { RequestsPerMinute = 300 } },
        { "Admin", new UserRoleLimit { RequestsPerMinute = 1000 } }
    };
}

/// <summary>
/// Rate limit configuration for a specific user role
/// </summary>
public class UserRoleLimit
{
    /// <summary>
    /// Maximum requests per minute for this role
    /// </summary>
    public int RequestsPerMinute { get; set; }

    /// <summary>
    /// Optional daily limit for this role
    /// </summary>
    public int? RequestsPerDay { get; set; }
}

/// <summary>
/// Endpoint-specific rate limiting rules
/// </summary>
public class EndpointRateLimitOptions
{
    /// <summary>
    /// Enable endpoint-specific rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Specific endpoint rules
    /// </summary>
    public List<EndpointRule> Rules { get; set; } = new()
    {
        new EndpointRule
        {
            Endpoint = "POST:/api/auth/login",
            RequestsPerMinute = 5,
            Description = "Login endpoint protection against brute force"
        },
        new EndpointRule
        {
            Endpoint = "GET:/api/export/*",
            RequestsPerMinute = 5,
            PerUser = true,
            Description = "Export endpoint protection against resource abuse"
        },
        new EndpointRule
        {
            Endpoint = "POST:/api/investigations/*/execute",
            RequestsPerMinute = 10,
            PerUser = true,
            Description = "Investigation execution rate limiting"
        }
    };
}

/// <summary>
/// Endpoint-specific rate limiting rule
/// </summary>
public class EndpointRule
{
    /// <summary>
    /// Endpoint pattern (supports wildcards)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Requests per minute for this endpoint
    /// </summary>
    public int RequestsPerMinute { get; set; }

    /// <summary>
    /// Apply limit per user instead of globally
    /// </summary>
    public bool PerUser { get; set; } = false;

    /// <summary>
    /// Optional description for this rule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Specific roles this rule applies to (empty means all roles)
    /// </summary>
    public List<string> ApplicableRoles { get; set; } = new();
}

/// <summary>
/// Feature flags for gradual rollout
/// </summary>
public class FeatureFlagOptions
{
    /// <summary>
    /// Enable IP-based rate limiting
    /// </summary>
    public bool EnableIpRateLimiting { get; set; } = true;

    /// <summary>
    /// Enable user-based rate limiting
    /// </summary>
    public bool EnableUserRateLimiting { get; set; } = true;

    /// <summary>
    /// Enable endpoint-specific rate limiting
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// Enable audit logging for rate limit events
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}