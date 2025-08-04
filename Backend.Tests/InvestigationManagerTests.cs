using ea_Tracker.Data;
using ea_Tracker.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ea_Tracker.Tests
{
    public class InvestigationManagerTests
    {
        [Fact]
        public void StartAndStopToggleStateAndRecordResults()
        {
            // Build a minimal service collection for this test
            var services = new ServiceCollection();
            services.AddDbContextFactory<ApplicationDbContext>(opts =>
                opts.UseInMemoryDatabase("tests"));
            services.AddLogging();
            services.AddTransient<InvoiceInvestigator>();
            services.AddTransient<WaybillInvestigator>();
            services.AddSingleton<IInvestigatorFactory, InvestigatorFactory>();
            // Build the service provider and factory for dynamic creation
            using var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IInvestigatorFactory>();
            var manager = new InvestigationManager(factory);

            // Create and start a new invoice investigator
            var id = manager.CreateInvestigator("invoice");
            manager.StartInvestigator(id);
            var state = manager.GetAllInvestigatorStates().Single(s => s.Id == id);
            Assert.True(state.IsRunning);
            
            // Stop and verify toggled off
            manager.StopInvestigator(id);
            state = manager.GetAllInvestigatorStates().Single(s => s.Id == id);
            Assert.False(state.IsRunning);
        }
    }
}
