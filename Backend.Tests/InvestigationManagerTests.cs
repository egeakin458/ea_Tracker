using ea_Tracker.Data;
using ea_Tracker.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ea_Tracker.Tests
{
    public class InvestigationManagerTests
    {
        [Fact]
        public void StartAndStopToggleStateAndRecordResults()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("tests")
                .Options;
            using var db = new ApplicationDbContext(options);
            var inv = new InvoiceInvestigator(db);
            var mgr = new InvestigationManager(new[] { inv });

            mgr.StartInvestigator(inv.Id);
            Assert.True(inv.IsRunning);
            Assert.NotEmpty(mgr.GetResults(inv.Id));

            mgr.StopInvestigator(inv.Id);
            Assert.False(inv.IsRunning);
        }
    }
}
