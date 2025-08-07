using ea_Tracker.Data;
using ea_Tracker.Services;
using ea_Tracker.Repositories;
using ea_Tracker.Models;
using ea_Tracker.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ea_Tracker.Tests
{
    public class InvestigationManagerTests
    {
        [Fact]
        public async Task CreateInvestigatorCreatesInDatabaseAsync()
        {
            // Build a minimal service collection for this test
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseInMemoryDatabase(Guid.NewGuid().ToString())); // Unique DB per test
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IInvestigatorRepository, InvestigatorRepository>();
            services.AddLogging();
            services.AddTransient<InvoiceInvestigator>();
            services.AddTransient<WaybillInvestigator>();
            // Register IInvestigatorRegistry for the factory pattern
            services.AddSingleton<IInvestigatorRegistry>(serviceProvider =>
            {
                var registry = new InvestigatorRegistry();
                registry.RegisterStandardTypes(serviceProvider);
                return registry;
            });
            services.AddScoped<IInvestigatorFactory, InvestigatorFactory>();
            services.AddScoped<InvestigationManager>();
            // Register IInvestigationConfiguration with in-memory config for test
            var configData = new Dictionary<string, string>
            {
                ["Investigation:Invoice:MaxTaxRatio"] = "0.5",
                ["Investigation:Invoice:CheckNegativeAmounts"] = "true",
                ["Investigation:Waybill:ExpiringSoonHours"] = "24",
                ["Investigation:Waybill:LegacyCutoffDays"] = "7"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData!).Build();
            services.AddSingleton<IInvestigationConfiguration>(new InvestigationConfiguration(config));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            
            // Setup test data - create investigator types
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            
            var invoiceType = new InvestigatorType
            {
                Code = "invoice",
                DisplayName = "Invoice Investigator",
                Description = "Investigates invoice anomalies",
                IsActive = true,
                DefaultConfiguration = "{}",
                CreatedAt = DateTime.UtcNow
            };
            context.InvestigatorTypes.Add(invoiceType);
            await context.SaveChangesAsync();

            var manager = scope.ServiceProvider.GetRequiredService<InvestigationManager>();

            // Create a new investigator instance
            var id = await manager.CreateInvestigatorAsync("invoice", "Test Invoice Investigator");
            
            // Verify it was created in the database
            var investigator = await context.InvestigatorInstances.FindAsync(id);
            Assert.NotNull(investigator);
            Assert.Equal("Test Invoice Investigator", investigator.CustomName);
            Assert.True(investigator.IsActive);
        }
    }
}
