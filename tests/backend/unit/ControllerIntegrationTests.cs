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
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Mapping;
using AutoMapper;
using Microsoft.Extensions.Configuration;

namespace ea_Tracker.Tests.Unit
{
    /// <summary>
    /// Integration tests for Controllers that verify end-to-end functionality with real repositories and database.
    /// Tests the integration between controllers, repositories, business logic, and data persistence.
    /// </summary>
    public class ControllerIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IWaybillService _waybillService;
        private readonly IInvoiceService _invoiceService;
        private readonly WaybillsController _waybillsController;
        private readonly InvoicesController _invoicesController;

        public ControllerIntegrationTests()
        {
            // Setup InMemory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            // Setup repositories
            var waybillRepository = new GenericRepository<Waybill>(_context);
            var invoiceRepository = new GenericRepository<Invoice>(_context);
            
            // Setup AutoMapper
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            var mapper = config.CreateMapper();
            
            // Setup Configuration
            var configData = new Dictionary<string, string>
            {
                {"Investigation:Invoice:MaxTaxRatio", "0.5"},
                {"Investigation:Invoice:MaxFutureDays", "0"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();
                
            // Setup service loggers
            var invoiceServiceLogger = new TestLogger<InvoiceService>();
            var waybillServiceLogger = new TestLogger<WaybillService>();
            
            _waybillService = new WaybillService(waybillRepository, mapper, configuration, waybillServiceLogger);
            _invoiceService = new InvoiceService(invoiceRepository, mapper, configuration, invoiceServiceLogger);

            // Create controllers with real dependencies
            var waybillLogger = new TestLogger<WaybillsController>();
            var invoiceLogger = new TestLogger<InvoicesController>();

            _waybillsController = new WaybillsController(_waybillService, waybillLogger);
            _invoicesController = new InvoicesController(_invoiceService, invoiceLogger);
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
            // The controller returns { errors = [...] }, so we just verify BadRequest was returned
            Assert.NotNull(badRequestResult.Value);

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
            // The controller returns { errors = [...] }, so we just verify BadRequest was returned
            Assert.NotNull(dueDateBadRequest.Value);
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
                    DueDate = DateTime.UtcNow.Date.AddDays(-1) // Overdue (yesterday)
                },
                new Waybill
                {
                    RecipientName = "Stats Test 2",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-5).Date,
                    WaybillType = WaybillType.Express,
                    HasAnomalies = false,
                    DueDate = DateTime.UtcNow.Date.AddHours(12) // Expiring soon (today but later)
                },
                new Waybill
                {
                    RecipientName = "Stats Test 3",
                    GoodsIssueDate = DateTime.UtcNow.AddDays(-2).Date,
                    WaybillType = WaybillType.Standard,
                    HasAnomalies = false,
                    DueDate = DateTime.UtcNow.Date.AddDays(5) // Normal (future)
                }
            };

            _context.Waybills.AddRange(testWaybills);
            await _context.SaveChangesAsync();

            // Get statistics
            var statsResult = await _waybillsController.GetWaybillStatistics();
            var statsOkResult = Assert.IsType<OkObjectResult>(statsResult.Result);
            
            // Verify statistics object structure and values
            var statsObject = statsOkResult.Value;
            Assert.NotNull(statsObject);

            // Use reflection to verify statistics properties
            var statsType = statsObject!.GetType();
            var totalWaybillsProp = statsType.GetProperty("TotalCount");
            var anomalousWaybillsProp = statsType.GetProperty("AnomalousCount");
            var overdueWaybillsProp = statsType.GetProperty("OverdueCount");
            var expiringSoonProp = statsType.GetProperty("ExpiringSoonCount");

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
            // The controller returns { errors = [...] }, so we need to check the structure
            Assert.NotNull(negativeBadRequest.Value);

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
            // The controller returns { errors = [...] }, so we just verify BadRequest was returned
            Assert.NotNull(excessiveTaxBadRequest.Value);

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
            // The controller returns { errors = [...] }, so we just verify BadRequest was returned
            Assert.NotNull(futureDateBadRequest.Value);
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
            var statsResult = await _invoicesController.GetInvoiceStatistics();
            var statsOkResult = Assert.IsType<OkObjectResult>(statsResult.Result);
            
            // Verify statistics object structure and values
            var statsObject = statsOkResult.Value;
            Assert.NotNull(statsObject);

            // Use reflection to verify statistics properties
            var statsType = statsObject!.GetType();
            var totalInvoicesProp = statsType.GetProperty("TotalCount");
            var anomalousInvoicesProp = statsType.GetProperty("AnomalousCount");
            var totalAmountProp = statsType.GetProperty("TotalAmount");
            var negativeAmountProp = statsType.GetProperty("NegativeAmountCount");

            Assert.NotNull(totalInvoicesProp);
            Assert.NotNull(anomalousInvoicesProp);
            Assert.NotNull(totalAmountProp);
            Assert.NotNull(negativeAmountProp);

            var totalInvoices = (int)totalInvoicesProp.GetValue(statsObject)!;
            var anomalousInvoices = (int)anomalousInvoicesProp.GetValue(statsObject)!;
            var totalAmount = (decimal)totalAmountProp.GetValue(statsObject)!;
            var negativeAmountCount = (int)negativeAmountProp.GetValue(statsObject)!;

            Assert.Equal(3, totalInvoices);
            Assert.Equal(1, anomalousInvoices);
            Assert.Equal(3500.00m, totalAmount);
            Assert.Equal(0, negativeAmountCount);
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
            // Test update not found (throws ValidationException, returns BadRequest with service layer)
            var updateResult = await _waybillsController.UpdateWaybill(99999, updateDto);
            Assert.IsType<BadRequestObjectResult>(updateResult.Result);

            // Test delete not found (returns BadRequest with service layer)
            var deleteResult = await _waybillsController.DeleteWaybill(99999);
            Assert.IsType<BadRequestObjectResult>(deleteResult);
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