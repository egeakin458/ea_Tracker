using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ea_Tracker.Exceptions;

namespace ea_Tracker.Middleware
{
    /// <summary>
    /// Enhanced exception handling middleware with security-conscious error responses.
    /// Prevents information leakage while providing appropriate error details for development.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Generate correlation ID for tracking
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            // Log the exception with correlation ID and sanitized context
            _logger.LogError(ex, 
                "Unhandled exception {CorrelationId} for {Method} {Path} from {RemoteIP}", 
                correlationId,
                context.Request.Method,
                context.Request.Path,
                GetSafeClientIp(context));

            // Determine response based on exception type
            var (statusCode, message, details) = GetErrorResponse(ex, correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Create secure error response
            var errorResponse = new
            {
                error = message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                details = _environment.IsDevelopment() ? details : null
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private (HttpStatusCode statusCode, string message, object? details) GetErrorResponse(Exception ex, string correlationId)
        {
            return ex switch
            {
                ea_Tracker.Exceptions.ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    "Validation failed",
                    new { errors = validationEx.Errors }
                ),
                
                ArgumentNullException => (
                    HttpStatusCode.BadRequest,
                    "Required parameter was not provided",
                    _environment.IsDevelopment() ? new { parameter = ex.Message } : null
                ),
                
                ArgumentException => (
                    HttpStatusCode.BadRequest,
                    "Invalid parameter provided",
                    _environment.IsDevelopment() ? new { error = ex.Message } : null
                ),
                
                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized,
                    "Access denied",
                    null
                ),
                
                SecurityException => (
                    HttpStatusCode.Forbidden,
                    "Security violation detected",
                    null
                ),
                
                InvalidOperationException => (
                    HttpStatusCode.BadRequest,
                    "Operation cannot be performed",
                    _environment.IsDevelopment() ? new { error = ex.Message } : null
                ),
                
                NotSupportedException => (
                    HttpStatusCode.NotImplemented,
                    "Operation not supported",
                    null
                ),
                
                TimeoutException => (
                    HttpStatusCode.RequestTimeout,
                    "Request timeout occurred",
                    null
                ),
                
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again later.",
                    _environment.IsDevelopment() ? new { 
                        type = ex.GetType().Name,
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    } : null
                )
            };
        }

        private static string GetSafeClientIp(HttpContext context)
        {
            try
            {
                // Check forwarded headers first (reverse proxy scenarios)
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    var firstIp = forwardedFor.Split(',')[0].Trim();
                    return AnonymizeIp(firstIp);
                }

                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return AnonymizeIp(realIp);
                }

                // Fall back to connection remote IP
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
                // Anonymize IP for privacy (keep first 3 octets for IPv4, first 6 groups for IPv6)
                if (ip.Contains('.')) // IPv4
                {
                    var parts = ip.Split('.');
                    if (parts.Length == 4)
                    {
                        return $"{parts[0]}.{parts[1]}.{parts[2]}.xxx";
                    }
                }
                else if (ip.Contains(':')) // IPv6
                {
                    var parts = ip.Split(':');
                    if (parts.Length >= 6)
                    {
                        return string.Join(":", parts.Take(6)) + ":xxxx:xxxx";
                    }
                }

                return "anonymized";
            }
            catch
            {
                return "anonymized";
            }
        }
    }
}
