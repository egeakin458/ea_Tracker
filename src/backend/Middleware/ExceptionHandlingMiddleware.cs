using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Middleware
{
    /// <summary>
    /// Catches unhandled exceptions and returns a standardized JSON error response.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Simple correlation id to help trace issues across logs and clients
            var correlationId = context.TraceIdentifier;

            var (statusCode, title) = exception switch
            {
                ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid argument"),
                ArgumentException => (HttpStatusCode.BadRequest, "Invalid request"),
                InvalidOperationException => (HttpStatusCode.BadRequest, "Business rule violation"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
                _ => (HttpStatusCode.InternalServerError, "Unexpected error")
            };

            _logger.LogError(exception, "{Title}. StatusCode: {StatusCode}. CorrelationId: {CorrelationId}", title, (int)statusCode, correlationId);

            var problem = new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}",
                title,
                status = (int)statusCode,
                detail = exception.Message,
                traceId = correlationId
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
