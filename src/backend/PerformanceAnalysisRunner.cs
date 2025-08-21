using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ea_Tracker.Extensions;
using ea_Tracker.Services;
using ea_Tracker.Data;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker
{
    /// <summary>
    /// Standalone program to run performance analysis and generate concrete evidence.
    /// </summary>
    public class PerformanceAnalysisRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("EA Tracker Performance Analysis Runner");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Build service provider
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add required services
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "InMemoryDatabase";
            
            services.AddDatabaseServices(connectionString);
            services.AddInvestigationServices();
            services.Configure<Microsoft.Extensions.Hosting.HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<PerformanceAnalysisRunner>>();
                    var profiler = scope.ServiceProvider.GetRequiredService<InvestigationPerformanceProfiler>();
                    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

                    logger.LogInformation("Setting up database with sample data...");
                    
                    // Ensure database is created and seeded with sample data
                    using (var context = dbContextFactory.CreateDbContext())
                    {
                        await context.Database.EnsureCreatedAsync();
                        
                        // Add sample data if database is empty
                        var invoiceCount = await context.Invoices.CountAsync();
                        var waybillCount = await context.Waybills.CountAsync();
                        
                        if (invoiceCount == 0 && waybillCount == 0)
                        {
                            logger.LogInformation("Database is empty, generating sample data...");
                            await GenerateSampleDataAsync(context, logger);
                        }
                        else
                        {
                            logger.LogInformation("Found existing data: {InvoiceCount} invoices, {WaybillCount} waybills", 
                                invoiceCount, waybillCount);
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("Running comprehensive performance analysis...");
                    Console.WriteLine();

                    // Run comprehensive analysis
                    var result = await profiler.PerformComprehensiveAnalysisAsync();
                    
                    Console.WriteLine("PERFORMANCE ANALYSIS RESULTS");
                    Console.WriteLine("============================");
                    Console.WriteLine();
                    Console.WriteLine($"Analysis Timestamp: {result.AnalysisTimestamp:yyyy-MM-dd HH:mm:ss UTC}");
                    Console.WriteLine();
                    
                    Console.WriteLine("DATABASE METRICS:");
                    Console.WriteLine($"  Total Records: {result.Database.TotalInvoices + result.Database.TotalWaybills} ({result.Database.TotalInvoices} invoices, {result.Database.TotalWaybills} waybills)");
                    Console.WriteLine($"  Invoice Load Time: {result.Database.InvoiceLoadTimeMs}ms");
                    Console.WriteLine($"  Waybill Load Time: {result.Database.WaybillLoadTimeMs}ms");
                    Console.WriteLine($"  Single Query Time: Invoice={result.Database.SingleInvoiceQueryTimeMs}ms, Waybill={result.Database.SingleWaybillQueryTimeMs}ms");
                    Console.WriteLine();
                    
                    Console.WriteLine("INVESTIGATION METRICS:");
                    Console.WriteLine($"  Invoice Investigation: {result.Investigation.InvoiceInvestigationTimeMs}ms ({result.Investigation.InvoiceAnomaliesFound} anomalies found)");
                    Console.WriteLine($"  Waybill Investigation: {result.Investigation.WaybillInvestigationTimeMs}ms ({result.Investigation.WaybillIssuesFound} issues found)");
                    Console.WriteLine($"  Total Execution Time: {result.Investigation.TotalExecutionTimeMs}ms");
                    Console.WriteLine($"  Results Generated: {result.Investigation.TotalResultsGenerated} ({result.Investigation.ResultsPerSecond:F1} results/second)");
                    Console.WriteLine();
                    
                    Console.WriteLine("COMPONENT BREAKDOWN:");
                    Console.WriteLine($"  Data Loading: {result.Components.DataLoadingTimeMs}ms ({(double)result.Components.DataLoadingTimeMs / result.Investigation.TotalExecutionTimeMs * 100:F1}%)");
                    Console.WriteLine($"  Business Logic: {result.Components.BusinessLogicTimeMs}ms ({(double)result.Components.BusinessLogicTimeMs / result.Investigation.TotalExecutionTimeMs * 100:F1}%)");
                    Console.WriteLine($"  Result Recording: {result.Components.ResultRecordingTimeMs}ms ({(double)result.Components.ResultRecordingTimeMs / result.Investigation.TotalExecutionTimeMs * 100:F1}%)");
                    Console.WriteLine($"  Database Saves: {result.Components.DatabaseSaveTimeMs}ms ({(double)result.Components.DatabaseSaveTimeMs / result.Investigation.TotalExecutionTimeMs * 100:F1}%)");
                    Console.WriteLine();
                    
                    Console.WriteLine("MEMORY METRICS:");
                    Console.WriteLine($"  Initial Memory: {result.Memory.InitialMemoryMB}MB");
                    Console.WriteLine($"  Peak Memory: {result.Memory.PeakMemoryMB}MB");
                    Console.WriteLine($"  Memory Delta: {result.Memory.MemoryDeltaMB}MB");
                    Console.WriteLine();

                    Console.WriteLine("PERFORMANCE SUMMARY:");
                    Console.WriteLine(result.Summary);
                    Console.WriteLine();

                    // Run claimed bottleneck analysis
                    Console.WriteLine();
                    Console.WriteLine("CLAIMED BOTTLENECK ANALYSIS");
                    Console.WriteLine("===========================");
                    var bottleneckAnalysis = await profiler.AnalyzeClaimedBottleneckAsync();
                    Console.WriteLine(bottleneckAnalysis);

                    Console.WriteLine();
                    Console.WriteLine("Analysis complete. Press any key to exit...");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during performance analysis: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        private static async Task GenerateSampleDataAsync(ApplicationDbContext context, ILogger logger)
        {
            var random = new Random();
            var sampleInvoices = new List<ea_Tracker.Models.Invoice>();
            var sampleWaybills = new List<ea_Tracker.Models.Waybill>();

            // Generate sample invoices
            for (int i = 1; i <= 100; i++)
            {
                sampleInvoices.Add(new ea_Tracker.Models.Invoice
                {
                    Id = i,
                    InvoiceType = ea_Tracker.Enums.InvoiceType.Standard,
                    TotalAmount = (decimal)(random.NextDouble() * 1000 + 50),
                    TotalTax = (decimal)(random.NextDouble() * 100 + 5),
                    IssueDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    RecipientName = $"Customer {i}",
                    HasAnomalies = random.NextDouble() < 0.1, // 10% chance of anomalies
                    LastInvestigatedAt = random.NextDouble() < 0.5 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null
                });
            }

            // Generate sample waybills
            for (int i = 1; i <= 150; i++)
            {
                var issueDate = DateTime.UtcNow.AddDays(-random.Next(0, 365));
                sampleWaybills.Add(new ea_Tracker.Models.Waybill
                {
                    Id = i,
                    WaybillType = ea_Tracker.Enums.WaybillType.Standard,
                    RecipientName = $"Recipient {i}",
                    ShippedItems = $"Items for order {i}",
                    GoodsIssueDate = issueDate,
                    DueDate = random.NextDouble() < 0.7 ? issueDate.AddDays(random.Next(1, 30)) : null,
                    HasAnomalies = random.NextDouble() < 0.15, // 15% chance of anomalies
                    LastInvestigatedAt = random.NextDouble() < 0.4 ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null
                });
            }

            context.Invoices.AddRange(sampleInvoices);
            context.Waybills.AddRange(sampleWaybills);
            
            await context.SaveChangesAsync();
            
            logger.LogInformation("Generated {InvoiceCount} sample invoices and {WaybillCount} sample waybills", 
                sampleInvoices.Count, sampleWaybills.Count);
        }
    }
}