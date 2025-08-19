
using ea_Tracker.Data;
using ea_Tracker.Extensions;
using ea_Tracker.Hubs;
using ea_Tracker.Services.Implementations;
using DotNetEnv;
using ea_Tracker.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Try to load from secret.env file if it exists (for backwards compatibility)
if (File.Exists("secret.env"))
{
    Env.Load("secret.env");
}

// Get connection string from configuration (user secrets in dev, appsettings in prod)
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

// For testing environment, allow in-memory database usage via configuration
if (string.IsNullOrWhiteSpace(connectionString))
{
    var environment = builder.Configuration["Environment"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    if (string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase))
    {
        // For testing, we'll use a connection string that indicates in-memory usage
        // The ServiceCollectionExtensions will handle this appropriately
        connectionString = "InMemoryDatabase";
    }
    else
    {
        throw new InvalidOperationException("Database connection string is not configured. Please set ConnectionStrings:DefaultConnection in user secrets or DEFAULT_CONNECTION environment variable.");
    }
}

// Register all services using extension methods
builder.Services.AddDatabaseServices(connectionString);
builder.Services.AddDomainServices();
builder.Services.AddInvestigationServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Auto-migrate database on startup
await EnsureDatabaseCreatedAsync(app);

// Global exception handling
app.UseMiddleware<ea_Tracker.Middleware.ExceptionHandlingMiddleware>();

// Rate limiting middleware (after exception handling, before authentication)
app.UseMiddleware<ea_Tracker.Middleware.RateLimitingMiddleware>();

// Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EA Tracker API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Security headers with CSP
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy - strict for production, relaxed for development
    if (app.Environment.IsDevelopment())
    {
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:3000 https://localhost:3000; " +
            "connect-src 'self' ws://localhost:* wss://localhost:*; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self' data:; " +
            "style-src 'self' 'unsafe-inline';";
    }
    else
    {
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-hashes'; " +
            "connect-src 'self'; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
            
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }
    
    await next();
});

// CORS must run before auth and before endpoint mapping for SignalR to negotiate correctly
var corsPolicy = app.Environment.IsDevelopment() ? "FrontendDev" : "Production";
app.UseCors(corsPolicy);

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint with detailed JSON response
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString("c"),
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    description = kvp.Value.Description,
                    duration = kvp.Value.Duration.ToString("c"),
                    exception = kvp.Value.Exception?.Message
                }
            )
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
});

// Map controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<InvestigationHub>("/hubs/investigations");

app.Run();

/// <summary>
/// Ensures the database is created and migrations are applied on startup.
/// This approach automatically handles database setup without manual intervention.
/// </summary>
static async Task EnsureDatabaseCreatedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    using var context = await contextFactory.CreateDbContextAsync();
    
    try
    {
        logger.LogInformation("Checking database connection and applying migrations...");
        
        // This will create the database if it doesn't exist and apply any pending migrations
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Database migration completed successfully.");
        
        // Log some stats for confirmation
        var investigatorTypeCount = await context.InvestigatorTypes.CountAsync();
        logger.LogInformation("Database ready. Found {Count} investigator types.", investigatorTypeCount);
        
        // Seed initial data
        logger.LogInformation("Starting database seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure database is created. Application will continue but may not function properly.");
        
        // In production, you might want to throw here to prevent startup with broken database
        // throw;
    }
}

// Make the Program class public for integration testing
public partial class Program { }
