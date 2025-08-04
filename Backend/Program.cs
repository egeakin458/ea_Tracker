
using ea_Tracker.Data;
using ea_Tracker.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using ea_Tracker.Middleware;

var builder = WebApplication.CreateBuilder(args);

Env.Load("secret.env");

string? connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DEFAULT_CONNECTION environment variable is not set.");
}

// Add EF Core factory for MySQL (so we can use DbContext in singleton services)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
    ));

// Register investigators and manager as singletons and host as background service
builder.Services.AddSingleton<Investigator, InvoiceInvestigator>();
builder.Services.AddSingleton<Investigator, WaybillInvestigator>();
builder.Services.AddSingleton<InvestigationManager>();
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
