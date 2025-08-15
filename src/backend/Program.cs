
using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Services;
using ea_Tracker.Repositories;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using ea_Tracker.Middleware;
using ea_Tracker.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Try to load from secret.env file if it exists (for backwards compatibility)
if (File.Exists("secret.env"))
{
    Env.Load("secret.env");
}

// Get connection string from configuration (user secrets in dev, appsettings in prod)
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured. Please set ConnectionStrings:DefaultConnection in user secrets or DEFAULT_CONNECTION environment variable.");
}

// Add EF Core factory for MySQL (so we can use DbContext in singleton services)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 42)) // Updated to match installed version
    ));

// Add regular DbContext for dependency injection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 42))
    ));

// Register repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IInvestigatorRepository, InvestigatorRepository>();

// Register investigation entity repositories for CompletedInvestigationService
builder.Services.AddScoped<IGenericRepository<InvestigationExecution>, GenericRepository<InvestigationExecution>>();
builder.Services.AddScoped<IGenericRepository<InvestigationResult>, GenericRepository<InvestigationResult>>();
builder.Services.AddScoped<IGenericRepository<InvestigatorInstance>, GenericRepository<InvestigatorInstance>>();

// Register AutoMapper for service layer DTO mapping
builder.Services.AddAutoMapper(typeof(ea_Tracker.Mapping.AutoMapperProfile));

// Register entity-specific service layer (Phase 1)
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IInvoiceService, ea_Tracker.Services.Implementations.InvoiceService>();
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IWaybillService, ea_Tracker.Services.Implementations.WaybillService>();

// Phase 3: Register Investigator and Completed Investigation services
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IInvestigatorAdminService, ea_Tracker.Services.Implementations.InvestigatorAdminService>();
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.ICompletedInvestigationService, ea_Tracker.Services.Implementations.CompletedInvestigationService>();

// Register business service interfaces (SOLID - Dependency Inversion Principle)
builder.Services.AddScoped<IInvestigationManager, InvestigationManager>();

// Phase 2: Business Logic Components (Pure Business Logic - No Infrastructure Dependencies)
builder.Services.AddScoped<InvoiceAnomalyLogic>();
builder.Services.AddScoped<WaybillDeliveryLogic>();

// Phase 2: Configuration System (Externalized Business Thresholds)
builder.Services.AddSingleton<IInvestigationConfiguration, InvestigationConfiguration>();

// Phase 2: Enhanced Factory Pattern (Registration-Based Strategy Pattern)
builder.Services.AddSingleton<IInvestigatorRegistry>(serviceProvider =>
{
    var registry = new InvestigatorRegistry();
    registry.RegisterStandardTypes(serviceProvider);
    return registry;
});

// Register refactored investigators with business logic injection
builder.Services.AddTransient<InvoiceInvestigator>();
builder.Services.AddTransient<WaybillInvestigator>();
builder.Services.AddScoped<IInvestigatorFactory, InvestigatorFactory>();
builder.Services.AddHostedService<InvestigationHostedService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add health checks with liveness and readiness checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is running"))
    .AddDbContextCheck<ApplicationDbContext>("database");

// Enable controller support and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Notification service
builder.Services.AddSingleton<IInvestigationNotificationService, InvestigationNotificationService>();

var app = builder.Build();

// Auto-migrate database on startup
await EnsureDatabaseCreatedAsync(app);

// Global exception handling
app.UseMiddleware<ea_Tracker.Middleware.ExceptionHandlingMiddleware>();

// Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// CORS must run before auth and before endpoint mapping for SignalR to negotiate correctly
app.UseCors("FrontendDev");
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure database is created. Application will continue but may not function properly.");
        
        // In production, you might want to throw here to prevent startup with broken database
        // throw;
    }
}
