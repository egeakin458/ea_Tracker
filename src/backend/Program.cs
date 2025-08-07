
using ea_Tracker.Data;
using ea_Tracker.Services;
using ea_Tracker.Repositories;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using ea_Tracker.Middleware;

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

// Register business service interfaces (SOLID - Dependency Inversion Principle)
builder.Services.AddScoped<IInvestigationManager, InvestigationManager>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IWaybillService, WaybillService>();

// Register investigators and factory as scoped to work with EF Core
 builder.Services.AddTransient<InvoiceInvestigator>();
 builder.Services.AddTransient<WaybillInvestigator>();
 builder.Services.AddScoped<IInvestigatorFactory, InvestigatorFactory>();
 builder.Services.AddHostedService<InvestigationHostedService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Enable controller support and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();
app.UseCors("AllowAll");
// Map controllers
app.MapControllers();

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
