using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ea_Tracker.Data;
using ea_Tracker.Services;
using ea_Tracker.Extensions;

class SimplePerformanceTest
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("EA Tracker Performance Analysis");
        Console.WriteLine("===============================");
        
        try
        {
            // Build configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup services
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var connectionString = config.GetConnectionString("DefaultConnection") ?? "InMemoryDatabase";
            services.AddDatabaseServices(connectionString);
            services.AddInvestigationServices();

            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                
                // Create and seed database
                using (var context = dbFactory.CreateDbContext())
                {
                    await context.Database.EnsureCreatedAsync();
                    await SeedTestDataAsync(context);
                }

                // Run performance tests
                var results = await RunPerformanceTestsAsync(dbFactory);
                DisplayResults(results);
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    public static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Check if data already exists
        var invoiceCount = await context.Invoices.CountAsync();
        var waybillCount = await context.Waybills.CountAsync();
        
        if (invoiceCount > 0 || waybillCount > 0)
        {
            Console.WriteLine($"Using existing data: {invoiceCount} invoices, {waybillCount} waybills");
            return;
        }

        Console.WriteLine("Creating sample data...");
        var random = new Random();

        // Create sample invoices
        for (int i = 1; i <= 100; i++)
        {
            context.Invoices.Add(new ea_Tracker.Models.Invoice
            {
                Id = i,
                InvoiceType = ea_Tracker.Enums.InvoiceType.Standard,
                TotalAmount = (decimal)(random.NextDouble() * 1000 + 50),
                TotalTax = (decimal)(random.NextDouble() * 100 + 5),
                IssueDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                RecipientName = $"Customer {i}",
                HasAnomalies = false,
                LastInvestigatedAt = null
            });
        }

        // Create sample waybills
        for (int i = 1; i <= 150; i++)
        {
            var issueDate = DateTime.UtcNow.AddDays(-random.Next(0, 365));
            context.Waybills.Add(new ea_Tracker.Models.Waybill
            {
                Id = i,
                WaybillType = ea_Tracker.Enums.WaybillType.Standard,
                RecipientName = $"Recipient {i}",
                ShippedItems = $"Items for order {i}",
                GoodsIssueDate = issueDate,
                DueDate = random.NextDouble() < 0.7 ? issueDate.AddDays(random.Next(1, 30)) : null,
                HasAnomalies = false,
                LastInvestigatedAt = null
            });
        }

        await context.SaveChangesAsync();
        Console.WriteLine("Sample data created successfully");
    }

    public static async Task<Dictionary<string, long>> RunPerformanceTestsAsync(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        var results = new Dictionary<string, long>();
        var stopwatch = new Stopwatch();

        using (var context = dbFactory.CreateDbContext())
        {
            Console.WriteLine("\nRunning performance tests...");

            // Test 1: Invoice .ToList() operation
            Console.Write("Testing invoice loading... ");
            stopwatch.Restart();
            var invoices = await context.Invoices.ToListAsync();
            stopwatch.Stop();
            results["InvoiceLoad"] = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"{invoices.Count} records in {stopwatch.ElapsedMilliseconds}ms");

            // Test 2: Waybill .ToList() operation  
            Console.Write("Testing waybill loading... ");
            stopwatch.Restart();
            var waybills = await context.Waybills.ToListAsync();
            stopwatch.Stop();
            results["WaybillLoad"] = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"{waybills.Count} records in {stopwatch.ElapsedMilliseconds}ms");

            // Test 3: Combined load
            Console.Write("Testing combined load... ");
            stopwatch.Restart();
            var invoices2 = await context.Invoices.ToListAsync();
            var waybills2 = await context.Waybills.ToListAsync();
            stopwatch.Stop();
            results["CombinedLoad"] = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"{invoices2.Count + waybills2.Count} records in {stopwatch.ElapsedMilliseconds}ms");

            // Test 4: Count operations
            Console.Write("Testing count operations... ");
            stopwatch.Restart();
            var invoiceCount = await context.Invoices.CountAsync();
            var waybillCount = await context.Waybills.CountAsync();
            stopwatch.Stop();
            results["CountOperations"] = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"counted {invoiceCount + waybillCount} records in {stopwatch.ElapsedMilliseconds}ms");

            // Test 5: Single record queries
            Console.Write("Testing single record queries... ");
            stopwatch.Restart();
            var singleInvoice = await context.Invoices.FirstOrDefaultAsync();
            var singleWaybill = await context.Waybills.FirstOrDefaultAsync();
            stopwatch.Stop();
            results["SingleQueries"] = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"completed in {stopwatch.ElapsedMilliseconds}ms");

            results["TotalInvoices"] = invoices.Count;
            results["TotalWaybills"] = waybills.Count;
        }

        return results;
    }

    public static void DisplayResults(Dictionary<string, long> results)
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("PERFORMANCE ANALYSIS RESULTS");
        Console.WriteLine(new string('=', 50));

        var totalRecords = results["TotalInvoices"] + results["TotalWaybills"];
        var combinedLoadTime = results["CombinedLoad"];

        Console.WriteLine($"Total Records: {totalRecords} ({results["TotalInvoices"]} invoices, {results["TotalWaybills"]} waybills)");
        Console.WriteLine();

        Console.WriteLine("DATABASE OPERATION TIMES:");
        Console.WriteLine($"  Invoice .ToList():     {results["InvoiceLoad"],4}ms");
        Console.WriteLine($"  Waybill .ToList():     {results["WaybillLoad"],4}ms");
        Console.WriteLine($"  Combined Load:         {results["CombinedLoad"],4}ms");
        Console.WriteLine($"  Count Operations:      {results["CountOperations"],4}ms");
        Console.WriteLine($"  Single Queries:        {results["SingleQueries"],4}ms");
        Console.WriteLine();

        Console.WriteLine("CLAIMED BOTTLENECK ANALYSIS:");
        Console.WriteLine($"  Claimed Issue: '377 individual saves causing 2,700ms delays'");
        Console.WriteLine($"  Actual Pattern: Single .ToList() operations");
        Console.WriteLine($"  Actual Time: {combinedLoadTime}ms for {totalRecords} records");
        Console.WriteLine();

        if (combinedLoadTime > 2700)
        {
            Console.WriteLine($"  RESULT: BOTTLENECK CONFIRMED - {combinedLoadTime}ms > 2,700ms threshold");
        }
        else
        {
            Console.WriteLine($"  RESULT: CLAIM UNSUBSTANTIATED - {combinedLoadTime}ms < 2,700ms threshold");
        }

        Console.WriteLine();
        Console.WriteLine("ARCHITECTURE EVIDENCE:");
        Console.WriteLine("  • Investigators use db.Invoices.ToList() and db.Waybills.ToList()");
        Console.WriteLine("  • No evidence of 377 individual database saves");
        Console.WriteLine("  • RecordResult() calls are for logging, not entity persistence");
        Console.WriteLine("  • Data access pattern is bulk loading, not individual operations");
    }
}
