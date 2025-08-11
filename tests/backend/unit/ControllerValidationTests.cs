using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ea_Tracker.Controllers;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Enums;
using System;
using System.Threading.Tasks;

namespace ea_Tracker.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for controller validation business logic.
    /// Tests all private validation methods through public API endpoints.
    /// Critical for ensuring refactored business logic maintains quality.
    /// </summary>
    public class ControllerValidationTests
    {
        private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepository;
        private readonly Mock<IGenericRepository<Waybill>> _mockWaybillRepository;
        private readonly Mock<ILogger<InvoicesController>> _mockInvoiceLogger;
        private readonly Mock<ILogger<WaybillsController>> _mockWaybillLogger;
        private readonly InvoicesController _invoicesController;
        private readonly WaybillsController _waybillsController;

        public ControllerValidationTests()
        {
            _mockInvoiceRepository = new Mock<IGenericRepository<Invoice>>();
            _mockWaybillRepository = new Mock<IGenericRepository<Waybill>>();
            _mockInvoiceLogger = new Mock<ILogger<InvoicesController>>();
            _mockWaybillLogger = new Mock<ILogger<WaybillsController>>();

            _invoicesController = new InvoicesController(_mockInvoiceRepository.Object, _mockInvoiceLogger.Object);
            _waybillsController = new WaybillsController(_mockWaybillRepository.Object, _mockWaybillLogger.Object);
        }

        #region Invoice Validation Tests

        [Fact]
        public async Task CreateInvoice_ValidInvoice_ReturnsCreated()
        {
            // Arrange
            var validCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Valid Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            var createdInvoice = new Invoice
            {
                Id = 1,
                RecipientName = validCreateDto.RecipientName,
                TotalAmount = validCreateDto.TotalAmount,
                IssueDate = validCreateDto.IssueDate,
                TotalTax = validCreateDto.TotalTax,
                InvoiceType = validCreateDto.InvoiceType,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                .ReturnsAsync(createdInvoice);

            // Act
            var result = await _invoicesController.CreateInvoice(validCreateDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var responseDto = Assert.IsType<InvoiceResponseDto>(createdResult.Value);
            Assert.Equal(validCreateDto.RecipientName, responseDto.RecipientName);
            Assert.Equal(validCreateDto.TotalAmount, responseDto.TotalAmount);
        }

        [Fact]
        public async Task CreateInvoice_NegativeAmount_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = -100.00m, // Invalid: negative amount
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Invoice amount cannot be negative", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_NegativeTax_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = -18.00m, // Invalid: negative tax
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Tax amount cannot be negative", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_TaxExceedsAmount_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 150.00m, // Invalid: tax exceeds amount
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Tax amount cannot exceed invoice amount", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_FutureIssueDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(1), // Invalid: future date
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Invoice issue date cannot be in the future", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_TooOldIssueDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddYears(-11), // Invalid: more than 10 years old
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Invoice issue date cannot be more than 10 years old", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_EmptyRecipientName_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "", // Invalid: empty recipient name
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Recipient name is required", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_RecipientNameTooLong_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = new string('A', 201), // Invalid: 201 characters, exceeds 200 limit
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Recipient name cannot exceed 200 characters", errorMessage);
        }

        [Fact]
        public async Task CreateInvoice_ZeroAmountWithTax_ReturnsCreated()
        {
            // Arrange - Edge case: zero amount with tax should be valid according to business rules
            var edgeCaseDto = new CreateInvoiceDto
            {
                RecipientName = "Edge Case Recipient",
                TotalAmount = 0.00m, // Edge case: zero amount
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 10.00m, // Tax with zero amount - should be allowed
                InvoiceType = InvoiceType.Standard
            };

            var createdInvoice = new Invoice
            {
                Id = 1,
                RecipientName = edgeCaseDto.RecipientName,
                TotalAmount = edgeCaseDto.TotalAmount,
                IssueDate = edgeCaseDto.IssueDate,
                TotalTax = edgeCaseDto.TotalTax,
                InvoiceType = edgeCaseDto.InvoiceType,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                .ReturnsAsync(createdInvoice);

            // Act
            var result = await _invoicesController.CreateInvoice(edgeCaseDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var responseDto = Assert.IsType<InvoiceResponseDto>(createdResult.Value);
            Assert.Equal(0.00m, responseDto.TotalAmount);
            Assert.Equal(10.00m, responseDto.TotalTax);
        }

        [Fact]
        public async Task DeleteInvoice_AnomalousInvestigatedInvoice_ReturnsBadRequest()
        {
            // Arrange
            var anomalousInvoice = new Invoice
            {
                Id = 1,
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-10),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard,
                HasAnomalies = true, // Has anomalies
                LastInvestigatedAt = DateTime.UtcNow.AddDays(-5), // Was investigated
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(anomalousInvoice);

            // Act
            var result = await _invoicesController.DeleteInvoice(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("business constraints", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteInvoice_OldInvoice_ReturnsBadRequest()
        {
            // Arrange
            var oldInvoice = new Invoice
            {
                Id = 1,
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-50),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard,
                HasAnomalies = false,
                LastInvestigatedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-31), // Older than 30 days
                UpdatedAt = DateTime.UtcNow.AddDays(-31)
            };

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(oldInvoice);

            // Act
            var result = await _invoicesController.DeleteInvoice(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("business constraints", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteInvoice_ValidInvoice_ReturnsNoContent()
        {
            // Arrange
            var validInvoice = new Invoice
            {
                Id = 1,
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-10),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard,
                HasAnomalies = false,
                LastInvestigatedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-15), // Less than 30 days old
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(validInvoice);

            // Act
            var result = await _invoicesController.DeleteInvoice(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockInvoiceRepository.Verify(r => r.Remove(validInvoice), Times.Once);
            _mockInvoiceRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Waybill Validation Tests

        [Fact]
        public async Task CreateWaybill_ValidWaybill_ReturnsCreated()
        {
            // Arrange
            var validCreateDto = new CreateWaybillDto
            {
                RecipientName = "Valid Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            var createdWaybill = new Waybill
            {
                Id = 1,
                RecipientName = validCreateDto.RecipientName,
                GoodsIssueDate = validCreateDto.GoodsIssueDate,
                WaybillType = validCreateDto.WaybillType,
                ShippedItems = validCreateDto.ShippedItems,
                DueDate = validCreateDto.DueDate,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockWaybillRepository.Setup(r => r.AddAsync(It.IsAny<Waybill>()))
                .ReturnsAsync(createdWaybill);

            // Act
            var result = await _waybillsController.CreateWaybill(validCreateDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var responseDto = Assert.IsType<WaybillResponseDto>(createdResult.Value);
            Assert.Equal(validCreateDto.RecipientName, responseDto.RecipientName);
            Assert.Equal(validCreateDto.GoodsIssueDate, responseDto.GoodsIssueDate);
        }

        [Fact]
        public async Task CreateWaybill_FutureGoodsIssueDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(1), // Invalid: future date
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Goods issue date cannot be in the future", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_TooOldGoodsIssueDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddYears(-6), // Invalid: more than 5 years old
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Goods issue date cannot be more than 5 years old", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_DueDateBeforeIssueDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(-2) // Invalid: due date before issue date
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Due date cannot be earlier than goods issue date", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_DueDateTooFarInFuture_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.AddYears(2) // Invalid: more than 1 year in future
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Due date cannot be more than 1 year in the future", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_EmptyRecipientName_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "", // Invalid: empty recipient name
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Recipient name is required", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_RecipientNameTooLong_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = new string('A', 201), // Invalid: 201 characters, exceeds 200 limit
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Recipient name cannot exceed 200 characters", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_ShippedItemsTooLong_ReturnsBadRequest()
        {
            // Arrange
            var invalidCreateDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = new string('A', 1001), // Invalid: 1001 characters, exceeds 1000 limit
                DueDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var result = await _waybillsController.CreateWaybill(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Shipped items description cannot exceed 1000 characters", errorMessage);
        }

        [Fact]
        public async Task CreateWaybill_NullDueDate_ReturnsCreated()
        {
            // Arrange - Edge case: null due date should be valid
            var edgeCaseDto = new CreateWaybillDto
            {
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-1),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = null // Edge case: null due date should be allowed
            };

            var createdWaybill = new Waybill
            {
                Id = 1,
                RecipientName = edgeCaseDto.RecipientName,
                GoodsIssueDate = edgeCaseDto.GoodsIssueDate,
                WaybillType = edgeCaseDto.WaybillType,
                ShippedItems = edgeCaseDto.ShippedItems,
                DueDate = edgeCaseDto.DueDate,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockWaybillRepository.Setup(r => r.AddAsync(It.IsAny<Waybill>()))
                .ReturnsAsync(createdWaybill);

            // Act
            var result = await _waybillsController.CreateWaybill(edgeCaseDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var responseDto = Assert.IsType<WaybillResponseDto>(createdResult.Value);
            Assert.Null(responseDto.DueDate);
        }

        [Fact]
        public async Task DeleteWaybill_AnomalousInvestigatedWaybill_ReturnsBadRequest()
        {
            // Arrange
            var anomalousWaybill = new Waybill
            {
                Id = 1,
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-10),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5),
                HasAnomalies = true, // Has anomalies
                LastInvestigatedAt = DateTime.UtcNow.AddDays(-5), // Was investigated
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            _mockWaybillRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(anomalousWaybill);

            // Act
            var result = await _waybillsController.DeleteWaybill(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("business constraints", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteWaybill_OldWaybill_ReturnsBadRequest()
        {
            // Arrange
            var oldWaybill = new Waybill
            {
                Id = 1,
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-50),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5),
                HasAnomalies = false,
                LastInvestigatedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-31), // Older than 30 days
                UpdatedAt = DateTime.UtcNow.AddDays(-31)
            };

            _mockWaybillRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(oldWaybill);

            // Act
            var result = await _waybillsController.DeleteWaybill(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("business constraints", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteWaybill_OverdueWaybill_ReturnsBadRequest()
        {
            // Arrange
            var overdueWaybill = new Waybill
            {
                Id = 1,
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-10),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(-1), // Overdue
                HasAnomalies = false,
                LastInvestigatedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-15), // Less than 30 days old
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            _mockWaybillRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(overdueWaybill);

            // Act
            var result = await _waybillsController.DeleteWaybill(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("business constraints", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteWaybill_ValidWaybill_ReturnsNoContent()
        {
            // Arrange
            var validWaybill = new Waybill
            {
                Id = 1,
                RecipientName = "Test Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-10),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Test items",
                DueDate = DateTime.UtcNow.Date.AddDays(5), // Not overdue
                HasAnomalies = false,
                LastInvestigatedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-15), // Less than 30 days old
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            _mockWaybillRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(validWaybill);

            // Act
            var result = await _waybillsController.DeleteWaybill(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockWaybillRepository.Verify(r => r.Remove(validWaybill), Times.Once);
            _mockWaybillRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Update Validation Tests

        [Fact]
        public async Task UpdateInvoice_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var existingInvoice = new Invoice
            {
                Id = 1,
                RecipientName = "Original Recipient",
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-5),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var validUpdateDto = new UpdateInvoiceDto
            {
                RecipientName = "Updated Recipient",
                TotalAmount = 200.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-3),
                TotalTax = 36.00m,
                InvoiceType = InvoiceType.Standard
            };

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingInvoice);

            // Act
            var result = await _invoicesController.UpdateInvoice(1, validUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseDto = Assert.IsType<InvoiceResponseDto>(okResult.Value);
            Assert.Equal(validUpdateDto.RecipientName, responseDto.RecipientName);
            Assert.Equal(validUpdateDto.TotalAmount, responseDto.TotalAmount);
            
            _mockInvoiceRepository.Verify(r => r.Update(existingInvoice), Times.Once);
            _mockInvoiceRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateWaybill_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var existingWaybill = new Waybill
            {
                Id = 1,
                RecipientName = "Original Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-10),
                WaybillType = WaybillType.Standard,
                ShippedItems = "Original items",
                DueDate = DateTime.UtcNow.Date.AddDays(5),
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var validUpdateDto = new UpdateWaybillDto
            {
                RecipientName = "Updated Recipient",
                GoodsIssueDate = DateTime.UtcNow.Date.AddDays(-8),
                WaybillType = WaybillType.Express,
                ShippedItems = "Updated items",
                DueDate = DateTime.UtcNow.Date.AddDays(7)
            };

            _mockWaybillRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingWaybill);

            // Act
            var result = await _waybillsController.UpdateWaybill(1, validUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseDto = Assert.IsType<WaybillResponseDto>(okResult.Value);
            Assert.Equal(validUpdateDto.RecipientName, responseDto.RecipientName);
            Assert.Equal(validUpdateDto.GoodsIssueDate, responseDto.GoodsIssueDate);
            
            _mockWaybillRepository.Verify(r => r.Update(existingWaybill), Times.Once);
            _mockWaybillRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Boundary Value Tests

        [Theory]
        [InlineData(-0.01)] // Just below zero
        [InlineData(-1.00)] // Negative value
        [InlineData(-999999.99)] // Large negative value
        public async Task CreateInvoice_NegativeAmountBoundaryTests_ReturnsBadRequest(decimal negativeAmount)
        {
            // Arrange
            var invalidCreateDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = negativeAmount,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 0.00m,
                InvoiceType = InvoiceType.Standard
            };

            // Act
            var result = await _invoicesController.CreateInvoice(invalidCreateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorMessage = badRequestResult.Value?.ToString();
            Assert.Contains("Invoice amount cannot be negative", errorMessage);
        }

        [Theory]
        [InlineData(199)] // Just under limit
        [InlineData(200)] // Exactly at limit
        public async Task CreateInvoice_RecipientNameLengthBoundaryTests_ReturnsCreated(int nameLength)
        {
            // Arrange
            var validCreateDto = new CreateInvoiceDto
            {
                RecipientName = new string('A', nameLength),
                TotalAmount = 100.00m,
                IssueDate = DateTime.UtcNow.Date.AddDays(-1),
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            var createdInvoice = new Invoice
            {
                Id = 1,
                RecipientName = validCreateDto.RecipientName,
                TotalAmount = validCreateDto.TotalAmount,
                IssueDate = validCreateDto.IssueDate,
                TotalTax = validCreateDto.TotalTax,
                InvoiceType = validCreateDto.InvoiceType,
                HasAnomalies = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                .ReturnsAsync(createdInvoice);

            // Act
            var result = await _invoicesController.CreateInvoice(validCreateDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdResult.Value);
        }

        [Theory]
        [InlineData(-9, -364)] // 9 years, 364 days ago - should be valid
        [InlineData(-10, 0)] // Exactly 10 years ago - should be valid
        [InlineData(-10, -1)] // 10 years, 1 day ago - should be invalid
        public async Task CreateInvoice_IssueDateBoundaryTests(int years, int days)
        {
            // Arrange
            var testDate = DateTime.UtcNow.Date.AddYears(years).AddDays(days);
            var createDto = new CreateInvoiceDto
            {
                RecipientName = "Test Recipient",
                TotalAmount = 100.00m,
                IssueDate = testDate,
                TotalTax = 18.00m,
                InvoiceType = InvoiceType.Standard
            };

            var shouldBeValid = years > -10 || (years == -10 && days >= 0);

            if (shouldBeValid)
            {
                var createdInvoice = new Invoice
                {
                    Id = 1,
                    RecipientName = createDto.RecipientName,
                    TotalAmount = createDto.TotalAmount,
                    IssueDate = createDto.IssueDate,
                    TotalTax = createDto.TotalTax,
                    InvoiceType = createDto.InvoiceType,
                    HasAnomalies = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                    .ReturnsAsync(createdInvoice);
            }

            // Act
            var result = await _invoicesController.CreateInvoice(createDto);

            // Assert
            if (shouldBeValid)
            {
                var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
                Assert.NotNull(createdResult.Value);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
                var errorMessage = badRequestResult.Value?.ToString();
                Assert.Contains("Invoice issue date cannot be more than 10 years old", errorMessage);
            }
        }

        #endregion
    }
}