using Xunit;
using ea_Tracker.Services;
using ea_Tracker.Models;
using Microsoft.Extensions.Configuration;

namespace ea_Tracker.Tests
{
    public class BusinessLogicTests
    {
        private IInvestigationConfiguration GetTestConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                ["Investigation:Invoice:MaxTaxRatio"] = "0.5",
                ["Investigation:Invoice:CheckNegativeAmounts"] = "true",
                ["Investigation:Waybill:ExpiringSoonHours"] = "24",
                ["Investigation:Waybill:LegacyCutoffDays"] = "7"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            return new InvestigationConfiguration(config);
        }

        [Fact]
        public void InvoiceAnomalyLogic_DetectsNegativeAmount()
        {
            // Arrange
            var logic = new InvoiceAnomalyLogic();
            var config = GetTestConfiguration();
            var invoice = new Invoice
            {
                Id = 1,
                TotalAmount = -100,
                TotalTax = 0,
                IssueDate = DateTime.UtcNow.AddDays(-1),
                RecipientName = "Test"
            };

            // Act
            var isAnomaly = logic.IsAnomaly(invoice, config);

            // Assert
            Assert.True(isAnomaly);
        }

        [Fact]
        public void WaybillDeliveryLogic_DetectsOverdue()
        {
            // Arrange
            var logic = new WaybillDeliveryLogic();
            var config = GetTestConfiguration();
            var waybill = new Waybill
            {
                Id = 1,
                DueDate = DateTime.UtcNow.AddDays(-1), // 1 day overdue
                GoodsIssueDate = DateTime.UtcNow.AddDays(-5),
                RecipientName = "Test"
            };

            // Act
            var isAnomaly = logic.IsAnomaly(waybill, config);

            // Assert
            Assert.True(isAnomaly);
        }
    }
}
