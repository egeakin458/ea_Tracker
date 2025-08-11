using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ea_Tracker.Controllers;
using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Enums;
using ea_Tracker.Repositories;

namespace ea_Tracker.Tests.Unit
{
    /// <summary>
    /// Integration tests for Controllers that verify end-to-end functionality with real repositories and database.
    /// Tests the integration between controllers, repositories, business logic, and data persistence.
    /// </summary>
    public class ControllerIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IGenericRepository<Waybill> _waybillRepository;
        private readonly IGenericRepository<Invoice> _invoiceRepository;
        private readonly WaybillsController _waybillsController;
        private readonly InvoicesController _invoicesController;

        public ControllerIntegrationTests()
        {
            // Setup InMemory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _waybillRepository = new GenericRepository<Waybill>(_context);
            _invoiceRepository = new GenericRepository<Invoice>(_context);

            // Create controllers with real dependencies
            var waybillLogger = new TestLogger<WaybillsController>();
            var invoiceLogger = new TestLogger<InvoicesController>();

            _waybillsController = new WaybillsController(_waybillRepository, waybillLogger);
            _invoicesController = new InvoicesController(_invoiceRepository, invoiceLogger);
        }

        #region WaybillsController Integration Tests

        [Fact]
        public async Task WaybillCrud_EndToEndWorkflow_WorksCorrectly()
        {
            // CREATE - Test waybill creation
            var createDto = new CreateWaybillDto
            {
                RecipientName = "Integration Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date,
                WaybillType = WaybillType.Standard,
                ShippedItems = "Integration test items",
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var createResult = await _waybillsController.CreateWaybill(createDto);

            // Assert creation success
            var createdActionResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdWaybill = Assert.IsType<WaybillResponseDto>(createdActionResult.Value);
            
            Assert.Equal(createDto.RecipientName, createdWaybill.RecipientName);
            Assert.Equal(createDto.GoodsIssueDate, createdWaybill.GoodsIssueDate);
            Assert.False(createdWaybill.HasAnomalies);

            var waybillId = createdWaybill.Id;

            // READ - Test waybill retrieval
            var getResult = await _waybillsController.GetWaybill(waybillId);
            
            var getOkResult = Assert.IsType<OkObjectResult>(getResult.Result);
            var retrievedWaybill = Assert.IsType<WaybillResponseDto>(getOkResult.Value);
            
            Assert.Equal(waybillId, retrievedWaybill.Id);
            Assert.Equal(createDto.RecipientName, retrievedWaybill.RecipientName);

            // UPDATE - Test waybill modification
            var updateDto = new UpdateWaybillDto
            {
                RecipientName = "Updated Integration Test Recipient",
                GoodsIssueDate = createDto.GoodsIssueDate,
                WaybillType = WaybillType.Express,
                ShippedItems = "Updated integration test items",
                DueDate = DateTime.UtcNow.AddDays(14)
            };

            var updateResult = await _waybillsController.UpdateWaybill(waybillId, updateDto);
            
            var updateOkResult = Assert.IsType<OkObjectResult>(updateResult.Result);
            var updatedWaybill = Assert.IsType<WaybillResponseDto>(updateOkResult.Value);
            
            Assert.Equal(updateDto.RecipientName, updatedWaybill.RecipientName);
            Assert.Equal(updateDto.WaybillType, updatedWaybill.WaybillType);
            Assert.True(updatedWaybill.UpdatedAt > updatedWaybill.CreatedAt);

            // DELETE - Test waybill removal (should work as it's a fresh, non-anomalous waybill)
            var deleteResult = await _waybillsController.DeleteWaybill(waybillId);
            Assert.IsType<NoContentResult>(deleteResult);

            // Verify deletion - should return NotFound
            var verifyResult = await _waybillsController.GetWaybill(waybillId);
            Assert.IsType<NotFoundObjectResult>(verifyResult.Result);
        }

        [Fact]
        public async Task WaybillValidation_BusinessRulesEnforced_PreventsInvalidOperations()
        {
            // Test future date validation
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Valid Recipient",
                GoodsIssueDate = DateTime.UtcNow.AddDays(1).Date, // Future date - invalid
                WaybillType = WaybillType.Standard
            };

            var result = await _waybillsController.CreateWaybill(invalidCreateDto);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("future", errorMessage);

            // Test due date before goods issue date
            var invalidDueDateDto = new CreateWaybillDto
            {
                RecipientName = "Valid Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date,
                WaybillType = WaybillType.Standard,
                DueDate = DateTime.UtcNow.AddDays(-1) // Before goods issue date - invalid
            };

            var dueDateResult = await _waybillsController.CreateWaybill(invalidDueDateDto);
            var dueDateBadRequest = Assert.IsType<BadRequestObjectResult>(dueDateResult.Result);
            var dueDateError = dueDateBadRequest.Value?.ToString();
            Assert.Contains("Due date cannot be earlier than goods issue date", dueDateError);
        }

        [Fact]
        public async Task WaybillFiltering_DatabaseIntegration_ReturnsFilteredResults()
        {
            // Seed test data directly to database
            var testWaybills = new[]
            {
                new Waybill
                {
                    RecipientName = "Alpha Company",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-5).Date,
                    WaybillType = WaybillType.Standard,
                    ShippedItems = "Alpha items",
                    HasAnomalies = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new Waybill
                {
                    RecipientName = "Beta Industries",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-3).Date,
                    WaybillType = WaybillType.Express,
                    ShippedItems = "Beta items",
                    HasAnomalies = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20)
                },
                new Waybill
                {
                    RecipientName = "Gamma Solutions",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-1).Date,
                    WaybillType = WaybillType.Standard,
                    ShippedItems = "Gamma items",
                    HasAnomalies = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _context.Waybills.AddRange(testWaybills);
            await _context.SaveChangesAsync();

            // Test filter by hasAnomalies
            var anomalousResult = await _waybillsController.GetWaybills(hasAnomalies: true);
            var anomalousOkResult = Assert.IsType<OkObjectResult>(anomalousResult.Result);
            var anomalousWaybills = Assert.IsAssignableFrom<IEnumerable<WaybillResponseDto>>(anomalousOkResult.Value);
            
            Assert.Single(anomalousWaybills);
            Assert.True(anomalousWaybills.First().HasAnomalies);

            // Test filter by recipient name (partial match)
            var alphaResult = await _waybillsController.GetWaybills(recipientName: "Alpha");
            var alphaOkResult = Assert.IsType<OkObjectResult>(alphaResult.Result);
            var alphaWaybills = Assert.IsAssignableFrom<IEnumerable<WaybillResponseDto>>(alphaOkResult.Value);
            
            Assert.Single(alphaWaybills);
            Assert.Contains("Alpha", alphaWaybills.First().RecipientName);

            // Test date range filtering
            var fromDate = DateTime.UtcNow.AddDays(-4);
            var toDate = DateTime.UtcNow.AddDays(-2);

            var dateRangeResult = await _waybillsController.GetWaybills(fromDate: fromDate, toDate: toDate);
            var dateRangeOkResult = Assert.IsType<OkObjectResult>(dateRangeResult.Result);
            var dateRangeWaybills = Assert.IsAssignableFrom<IEnumerable<WaybillResponseDto>>(dateRangeOkResult.Value);
            
            Assert.Single(dateRangeWaybills);
            Assert.True(dateRangeWaybills.First().GoodsIssueDate >= fromDate.Date);
            Assert.True(dateRangeWaybills.First().GoodsIssueDate <= toDate.Date);
        }

        [Fact]
        public async Task WaybillStats_DatabaseCalculation_ReturnsAccurateStatistics()
        {
            // Seed test data with known statistics
            var testWaybills = new[]
            {
                new Waybill
                {
                    RecipientName = "Stats Test 1",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-10).Date,
                    WaybillType = WaybillType.Standard,
                    HasAnomalies = true,
                    DueDate = DateTime.UtcNow.AddDays(-1) // Overdue
                },
                new Waybill
                {
                    RecipientName = "Stats Test 2",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-5).Date,
                    WaybillType = WaybillType.Express,
                    HasAnomalies = false,
                    DueDate = DateTime.UtcNow.AddHours(12) // Expiring soon
                },
                new Waybill
                {
                    RecipientName = "Stats Test 3",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-2).Date,
                    WaybillType = WaybillType.Standard,
                    HasAnomalies = false,
                    DueDate = DateTime.UtcNow.AddDays(5) // Normal
                }
            };

            _context.Waybills.AddRange(testWaybills);
            await _context.SaveChangesAsync();

            // Get statistics
            var statsResult = await _waybillsController.GetWaybillStats();
            var statsOkResult = Assert.IsType<OkObjectResult>(statsResult.Result);
            
            // Verify statistics object structure and values
            var statsObject = statsOkResult.Value;
            Assert.NotNull(statsObject);

            // Use reflection to verify statistics properties
            var statsType = statsObject!.GetType();
            var totalWaybillsProp = statsType.GetProperty("TotalWaybills");
            var anomalousWaybillsProp = statsType.GetProperty("AnomalousWaybills");
            var overdueWaybillsProp = statsType.GetProperty("OverdueWaybills");
            var expiringSoonProp = statsType.GetProperty("ExpiringSoonWaybills");

            Assert.NotNull(totalWaybillsProp);
            Assert.NotNull(anomalousWaybillsProp);
            Assert.NotNull(overdueWaybillsProp);
            Assert.NotNull(expiringSoonProp);

            var totalWaybills = (int)totalWaybillsProp.GetValue(statsObject)!;
            var anomalousWaybills = (int)anomalousWaybillsProp.GetValue(statsObject)!;
            var overdueWaybills = (int)overdueWaybillsProp.GetValue(statsObject)!;
            var expiringSoon = (int)expiringSoonProp.GetValue(statsObject)!;

            Assert.Equal(3, totalWaybills);
            Assert.Equal(1, anomalousWaybills);
            Assert.Equal(1, overdueWaybills);
            Assert.Equal(1, expiringSoon);
        }

        #endregion

        #region InvoicesController Integration Tests

        [Fact]
        public async Task InvoiceCrud_EndToEndWorkflow_WorksCorrectly()
        {
            // CREATE - Test invoice creation
            var createDto = new CreateInvoiceDto
            {
                RecipientName = "Integration Test Invoice Recipient",
                TotalAmount = 1500.00m,
                IssueDate = DateTime.UtcNow.Date,
                TotalTax = 150.00m,
                InvoiceType = InvoiceType.Standard
            };

            var createResult = await _invoicesController.CreateInvoice(createDto);

            // Assert creation success
            var createdActionResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdInvoice = Assert.IsType<InvoiceResponseDto>(createdActionResult.Value);
            
            Assert.Equal(createDto.RecipientName, createdInvoice.RecipientName);
            Assert.Equal(createDto.TotalAmount, createdInvoice.TotalAmount);
            Assert.False(createdInvoice.HasAnomalies);

            var invoiceId = createdInvoice.Id;

            // READ - Test invoice retrieval
            var getResult = await _invoicesController.GetInvoice(invoiceId);
            
            var getOkResult = Assert.IsType<OkObjectResult>(getResult.Result);
            var retrievedInvoice = Assert.IsType<InvoiceResponseDto>(getOkResult.Value);
            
            Assert.Equal(invoiceId, retrievedInvoice.Id);
            Assert.Equal(createDto.RecipientName, retrievedInvoice.RecipientName);

            // UPDATE - Test invoice modification
            var updateDto = new UpdateInvoiceDto
            {
                RecipientName = "Updated Integration Test Invoice Recipient",
                TotalAmount = 2000.00m,
                IssueDate = createDto.IssueDate,
                TotalTax = 200.00m,
                InvoiceType = InvoiceType.Credit
            };

            var updateResult = await _invoicesController.UpdateInvoice(invoiceId, updateDto);
            
            var updateOkResult = Assert.IsType<OkObjectResult>(updateResult.Result);
            var updatedInvoice = Assert.IsType<InvoiceResponseDto>(updateOkResult.Value);
            
            Assert.Equal(updateDto.RecipientName, updatedInvoice.RecipientName);
            Assert.Equal(updateDto.TotalAmount, updatedInvoice.TotalAmount);
            Assert.True(updatedInvoice.UpdatedAt > updatedInvoice.CreatedAt);

            // DELETE - Test invoice removal (should work as it's a fresh, non-anomalous invoice)
            var deleteResult = await _invoicesController.DeleteInvoice(invoiceId);
            Assert.IsType<NoContentResult>(deleteResult);

            // Verify deletion - should return NotFound
            var verifyResult = await _invoicesController.GetInvoice(invoiceId);
            Assert.IsType<NotFoundObjectResult>(verifyResult.Result);
        }

        [Fact]
        public async Task InvoiceValidation_BusinessRulesEnforced_PreventsInvalidOperations()
        {
            // Test negative amount validation
            var negativeAmountDto = new CreateInvoiceDto
            {
                RecipientName = "Valid Recipient",
                TotalAmount = -100.00m, // Negative amount - invalid
                IssueDate = DateTime.UtcNow.Date,
                TotalTax = 10.00m,
                InvoiceType = InvoiceType.Standard
            };

            var negativeResult = await _invoicesController.CreateInvoice(negativeAmountDto);
            var negativeBadRequest = Assert.IsType<BadRequestObjectResult>(negativeResult.Result);
            var negativeError = negativeBadRequest.Value?.ToString();
            Assert.Contains("Invoice amount cannot be negative", negativeError);

            // Test tax exceeding amount validation
            var excessiveTaxDto = new CreateInvoiceDto
            {
                RecipientName = "Valid Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date,
                TotalTax = 200.00m, // Tax exceeds amount - invalid
                InvoiceType = InvoiceType.Standard
            };

            var excessiveTaxResult = await _invoicesController.CreateInvoice(excessiveTaxDto);
            var excessiveTaxBadRequest = Assert.IsType<BadRequestObjectResult>(excessiveTaxResult.Result);
            var excessiveTaxError = excessiveTaxBadRequest.Value?.ToString();
            Assert.Contains("Tax amount cannot exceed invoice amount", excessiveTaxError);

            // Test future date validation
            var futureDateDto = new CreateInvoiceDto
            {
                RecipientName = "Valid Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.AddDays(1).Date, // Future date - invalid
                TotalTax = 10.00m,
                InvoiceType = InvoiceType.Standard
            };

            var futureDateResult = await _invoicesController.CreateInvoice(futureDateDto);
            var futureDateBadRequest = Assert.IsType<BadRequestObjectResult>(futureDateResult.Result);
            var futureDateError = futureDateBadRequest.Value?.ToString();
            Assert.Contains("Invoice issue date cannot be in the future", futureDateError);
        }

        [Fact]
        public async Task InvoiceStats_DatabaseCalculation_ReturnsAccurateStatistics()
        {
            // Seed test data with known statistics
            var testInvoices = new[]
            {
                new Invoice
                {
                    RecipientName = "Invoice Stats Test 1",
                    TotalAmount = 1000.00m,
                    IssueDate = DateTime.UtcNow.AddDays(-5).Date,
                    TotalTax = 100.00m,
                    InvoiceType = InvoiceType.Standard,
                    HasAnomalies = true
                },
                new Invoice
                {
                    RecipientName = "Invoice Stats Test 2",
                    TotalAmount = 2000.00m,
                    IssueDate = DateTime.UtcNow.AddDays(-3).Date,
                    TotalTax = 200.00m,
                    InvoiceType = InvoiceType.Credit,
                    HasAnomalies = false
                },
                new Invoice
                {
                    RecipientName = "Invoice Stats Test 3",
                    TotalAmount = 500.00m,
                    IssueDate = DateTime.UtcNow.AddDays(-1).Date,
                    TotalTax = 50.00m,
                    InvoiceType = InvoiceType.Debit,
                    HasAnomalies = false
                }
            };

            _context.Invoices.AddRange(testInvoices);
            await _context.SaveChangesAsync();

            // Get statistics
            var statsResult = await _invoicesController.GetInvoiceStats();
            var statsOkResult = Assert.IsType<OkObjectResult>(statsResult.Result);
            
            // Verify statistics object structure and values
            var statsObject = statsOkResult.Value;
            Assert.NotNull(statsObject);

            // Use reflection to verify statistics properties
            var statsType = statsObject!.GetType();
            var totalInvoicesProp = statsType.GetProperty("TotalInvoices");
            var anomalousInvoicesProp = statsType.GetProperty("AnomalousInvoices");
            var totalAmountProp = statsType.GetProperty("TotalAmount");
            var totalTaxProp = statsType.GetProperty("TotalTax");

            Assert.NotNull(totalInvoicesProp);
            Assert.NotNull(anomalousInvoicesProp);
            Assert.NotNull(totalAmountProp);
            Assert.NotNull(totalTaxProp);

            var totalInvoices = (int)totalInvoicesProp.GetValue(statsObject)!;
            var anomalousInvoices = (int)anomalousInvoicesProp.GetValue(statsObject)!;
            var totalAmount = (decimal)totalAmountProp.GetValue(statsObject)!;
            var totalTax = (decimal)totalTaxProp.GetValue(statsObject)!;

            Assert.Equal(3, totalInvoices);
            Assert.Equal(1, anomalousInvoices);
            Assert.Equal(3500.00m, totalAmount);
            Assert.Equal(350.00m, totalTax);
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public async Task Controllers_DatabaseErrors_HandleExceptionsGracefully()
        {
            // Dispose the context to simulate database connection issues
            await _context.DisposeAsync();

            // Test that controllers handle database exceptions gracefully
            var result = await _waybillsController.GetWaybills();
            
            // Should return 500 Internal Server Error for database issues
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Controllers_NotFoundScenarios_ReturnCorrectStatusCodes()
        {
            // Test waybill not found
            var waybillResult = await _waybillsController.GetWaybill(99999);
            Assert.IsType<NotFoundObjectResult>(waybillResult.Result);

            // Test invoice not found
            var invoiceResult = await _invoicesController.GetInvoice(99999);
            Assert.IsType<NotFoundObjectResult>(invoiceResult.Result);

            // Test update not found
            var updateDto = new UpdateWaybillDto
            {
                RecipientName = "Test Update",
                GoodsIssueDate = DateTime.UtcNow.Date,
                WaybillType = WaybillType.Standard
            };
            var updateResult = await _waybillsController.UpdateWaybill(99999, updateDto);
            Assert.IsType<NotFoundObjectResult>(updateResult.Result);

            // Test delete not found
            var deleteResult = await _waybillsController.DeleteWaybill(99999);
            Assert.IsType<NotFoundObjectResult>(deleteResult);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    /// <summary>
    /// Simple test logger implementation for integration testing.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // No-op implementation for testing
        }
    }
}