using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Services;
using ea_Tracker.Services.Authentication;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Repositories;
using ea_Tracker.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ea_Tracker.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection to organize service registration.
    /// Follows Single Responsibility Principle by grouping related services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers database-related services (DbContext, repositories).
        /// Supports both MySQL and in-memory database for testing.
        /// </summary>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, string connectionString)
        {
            // Check if this is a test environment that should use in-memory database
            if (connectionString == "InMemoryDatabase")
            {
                // Use in-memory database for testing
                var databaseName = $"TestDatabase_{Guid.NewGuid()}";
                
                services.AddDbContextFactory<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
            }
            else
            {
                // Use MySQL for production/development
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 42));

                // Add EF Core factory for MySQL (for singleton services)
                services.AddDbContextFactory<ApplicationDbContext>(options =>
                    options.UseMySql(connectionString, serverVersion));

                // Add regular DbContext for dependency injection
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseMySql(connectionString, serverVersion));
            }

            // Register repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IInvestigatorRepository, InvestigatorRepository>();

            // Register investigation entity repositories
            services.AddScoped<IGenericRepository<InvestigationExecution>, GenericRepository<InvestigationExecution>>();
            services.AddScoped<IGenericRepository<InvestigationResult>, GenericRepository<InvestigationResult>>();
            services.AddScoped<IGenericRepository<InvestigatorInstance>, GenericRepository<InvestigatorInstance>>();

            return services;
        }

        /// <summary>
        /// Registers business domain services and their interfaces.
        /// </summary>
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Entity-specific services
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IWaybillService, WaybillService>();

            // Investigation services
            services.AddScoped<IInvestigatorAdminService, InvestigatorAdminService>();
            services.AddScoped<ICompletedInvestigationService, CompletedInvestigationService>();

            // Business manager
            services.AddScoped<IInvestigationManager, InvestigationManager>();

            // Notification service
            services.AddSingleton<IInvestigationNotificationService, InvestigationNotificationService>();

            return services;
        }

        /// <summary>
        /// Registers investigation-specific services (business logic, configuration, factory).
        /// </summary>
        public static IServiceCollection AddInvestigationServices(this IServiceCollection services)
        {
            // Business logic components (pure business logic)
            services.AddScoped<InvoiceAnomalyLogic>();
            services.AddScoped<WaybillDeliveryLogic>();

            // Configuration system (maintain backward compatibility)
            services.AddSingleton<IInvestigationConfiguration, InvestigationConfiguration>();

            // Enhanced strongly-typed configuration system
            services.AddSingleton<EnhancedInvestigationConfiguration>();
            
            // Configure strongly-typed options
            services.Configure<InvoiceInvestigationOptions>(options => 
            {
                // Default values are set in the class, but can be overridden by configuration
            });
            services.Configure<WaybillInvestigationOptions>(options => 
            {
                // Default values are set in the class, but can be overridden by configuration  
            });
            services.Configure<GeneralInvestigationOptions>(options => 
            {
                // Default values are set in the class, but can be overridden by configuration
            });

            // Generic investigation services
            services.AddScoped<IGenericInvestigationService<Invoice>, GenericInvestigationService<Invoice>>();
            services.AddScoped<IGenericInvestigationService<Waybill>, GenericInvestigationService<Waybill>>();
            services.AddSingleton<IGenericInvestigationServiceFactory, GenericInvestigationServiceFactory>();

            // Factory pattern implementation
            services.AddSingleton<IInvestigatorRegistry>(serviceProvider =>
            {
                var registry = new InvestigatorRegistry();
                registry.RegisterStandardTypes(serviceProvider);
                return registry;
            });

            // Investigators
            services.AddTransient<InvoiceInvestigator>();
            services.AddTransient<WaybillInvestigator>();
            services.AddScoped<IInvestigatorFactory, InvestigatorFactory>();

            // Background service
            services.AddHostedService<InvestigationHostedService>();

            return services;
        }

        /// <summary>
        /// Registers authentication services with JWT token support.
        /// </summary>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register JWT authentication service
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            
            // Register user management service
            services.AddScoped<IUserService, UserService>();
            
            // Register database seeder
            services.AddScoped<DatabaseSeeder>();

            // Retrieve JWT configuration with testing fallback
            var jwtSecretKey = configuration["Jwt:SecretKey"] ?? 
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
                
            // For testing environment, provide a safe fallback if no key is configured
            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                                configuration["ASPNETCORE_ENVIRONMENT"];
                
                if (environment == "Testing" || environment == "Development")
                {
                    jwtSecretKey = "this-is-a-test-secret-key-for-unit-testing-purposes-with-sufficient-length";
                }
                else
                {
                    throw new InvalidOperationException("JWT secret key not configured. For production, set JWT_SECRET_KEY environment variable or Jwt:SecretKey configuration.");
                }
            }
            
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "ea_tracker_api";
            var jwtAudience = configuration["Jwt:Audience"] ?? "ea_tracker_client";

            // Validate JWT secret key strength for production
            ValidateJwtSecretKeyStrength(jwtSecretKey, configuration);
            
            var key = Encoding.UTF8.GetBytes(jwtSecretKey);

            // Configure JWT authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !IsDevelopment(configuration); // Require HTTPS in production only
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew for production
                    RequireExpirationTime = true
                };

                // Configure SignalR JWT authentication
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Check for token in SignalR query string
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerHandler>>();
                        logger?.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserAccess", policy => policy.RequireRole("User", "Admin"));
            });

            return services;
        }

        /// <summary>
        /// Registers rate limiting services and configuration.
        /// </summary>
        public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure rate limiting options
            services.Configure<ea_Tracker.Configuration.RateLimitingOptions>(
                configuration.GetSection(ea_Tracker.Configuration.RateLimitingOptions.SectionName));

            // Add memory cache for rate limiting
            services.AddMemoryCache();

            return services;
        }

        /// <summary>
        /// Registers web-specific services with environment-aware CORS and security headers.
        /// </summary>
        public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Environment-specific CORS configuration
            services.AddCors(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.AddPolicy("FrontendDev", policy =>
                    {
                        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
                }
                else
                {
                    // Production CORS - restrict to specific domains
                    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    
                    options.AddPolicy("Production", policy =>
                    {
                        if (allowedOrigins.Length > 0)
                        {
                            policy.WithOrigins(allowedOrigins)
                                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                                  .WithHeaders("Content-Type", "Authorization")
                                  .AllowCredentials();
                        }
                        else
                        {
                            // Fallback to same-origin if no origins configured
                            policy.WithOrigins()
                                  .AllowAnyMethod()
                                  .AllowAnyHeader();
                        }
                    });
                }
            });

            // Health checks with enhanced database monitoring
            services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is running"))
                .AddDbContextCheck<ApplicationDbContext>("database");

            // Web API services with security configurations
            services.AddControllers();

            services.AddEndpointsApiExplorer();
            
            // Configure Swagger with JWT support
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "EA Tracker API",
                    Version = "v1",
                    Description = "Enterprise Anomaly Tracker API with JWT authentication"
                });

                // Add JWT authentication to Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // SignalR with enhanced security
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
                options.MaximumReceiveMessageSize = 32 * 1024; // 32KB limit
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
            });

            // AutoMapper
            services.AddAutoMapper(typeof(ea_Tracker.Mapping.AutoMapperProfile));

            return services;
        }

        /// <summary>
        /// Helper method to determine if we're in development environment
        /// </summary>
        private static bool IsDevelopment(IConfiguration configuration)
        {
            var environment = configuration["Environment"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Validates JWT secret key strength for production environments.
        /// </summary>
        private static void ValidateJwtSecretKeyStrength(string jwtSecretKey, IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                            configuration["ASPNETCORE_ENVIRONMENT"];
            
            // Skip validation for testing/development environments
            if (environment == "Testing" || environment == "Development")
                return;
                
            // Production security validation
            if (jwtSecretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long for production security.");
            }
            
            // Check for common weak patterns
            var weakPatterns = new[] { "secret", "password", "key", "token", "jwt", "default" };
            var lowerKey = jwtSecretKey.ToLowerInvariant();
            
            foreach (var pattern in weakPatterns)
            {
                if (lowerKey.Contains(pattern))
                {
                    throw new InvalidOperationException($"JWT secret key contains weak pattern '{pattern}'. Use a cryptographically secure random key.");
                }
            }
            
            // Ensure sufficient complexity (at least some variety in characters)
            var hasLetter = jwtSecretKey.Any(char.IsLetter);
            var hasDigit = jwtSecretKey.Any(char.IsDigit);
            var hasSpecial = jwtSecretKey.Any(c => !char.IsLetterOrDigit(c));
            
            if (!hasLetter || !hasDigit || !hasSpecial)
            {
                throw new InvalidOperationException("JWT secret key must contain letters, digits, and special characters for production security.");
            }
        }
    }
}