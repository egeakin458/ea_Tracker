using Xunit;
using Moq;
using ea_Tracker.Services.Implementations;
using ea_Tracker.Repositories;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Enums;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System;

namespace ea_Tracker.Tests.Unit
{
    public class CompletedInvestigationServiceExportTests
    {
        private readonly Mock<IGenericRepository<InvestigationExecution>> _mockExecutionRepo;
        private readonly Mock<IGenericRepository<InvestigationResult>> _mockResultRepo;
        private readonly Mock<IGenericRepository<InvestigatorInstance>> _mockInvestigatorRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CompletedInvestigationService>> _mockLogger;
        private readonly CompletedInvestigationService _service;

        public CompletedInvestigationServiceExportTests()
        {
            _mockExecutionRepo = new Mock<IGenericRepository<InvestigationExecution>>();
            _mockResultRepo = new Mock<IGenericRepository<InvestigationResult>>();
            _mockInvestigatorRepo = new Mock<IGenericRepository<InvestigatorInstance>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CompletedInvestigationService>>();
            
            _service = new CompletedInvestigationService(
                _mockExecutionRepo.Object,
                _mockResultRepo.Object,
                _mockInvestigatorRepo.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ExportInvestigationsAsync_ValidJsonRequest_ReturnsExportDto()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 1, 2 }, "json");
            var executions = new List<InvestigationExecution>
            {
                new InvestigationExecution 
                { 
                    Id = 1, 
                    InvestigatorId = Guid.NewGuid(),
                    ResultCount = 10,
                    StartedAt = DateTime.UtcNow.AddHours(-1),
                    CompletedAt = DateTime.UtcNow,
                    Investigator = new InvestigatorInstance { CustomName = "Test Investigation 1" }
                },
                new InvestigationExecution 
                { 
                    Id = 2, 
                    InvestigatorId = Guid.NewGuid(),
                    ResultCount = 5,
                    StartedAt = DateTime.UtcNow.AddHours(-2),
                    CompletedAt = DateTime.UtcNow.AddHours(-1),
                    Investigator = new InvestigatorInstance { CustomName = "Test Investigation 2" }
                }
            };

            _mockExecutionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationExecution, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationExecution>, IOrderedQueryable<InvestigationExecution>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(executions);

            _mockResultRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<InvestigationResult, bool>>>()))
                .ReturnsAsync(3);

            _mockResultRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationResult, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationResult>, IOrderedQueryable<InvestigationResult>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationResult>());

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/json", result.ContentType);
            Assert.Contains("investigations_export", result.FileName);
            Assert.Contains(".json", result.FileName);
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task ExportInvestigationsAsync_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 1 }, "invalid");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.ExportInvestigationsAsync(request));
        }

        [Fact]
        public async Task ExportInvestigationsAsync_EmptyIds_ReturnsNull()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int>(), "json");

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExportInvestigationsAsync_NullRequest_ReturnsNull()
        {
            // Arrange
            BulkExportRequestDto request = null;

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExportInvestigationsAsync_CsvFormat_ReturnsCsvFile()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 1 }, "csv");
            var execution = new InvestigationExecution 
            { 
                Id = 1, 
                InvestigatorId = Guid.NewGuid(),
                ResultCount = 1,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Investigator = new InvestigatorInstance { CustomName = "Test" }
            };

            _mockExecutionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationExecution, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationExecution>, IOrderedQueryable<InvestigationExecution>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationExecution> { execution });

            _mockResultRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<InvestigationResult, bool>>>()))
                .ReturnsAsync(0);

            _mockResultRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationResult, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationResult>, IOrderedQueryable<InvestigationResult>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationResult>());

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("text/csv", result.ContentType);
            Assert.Contains(".csv", result.FileName);
        }

        [Fact]
        public async Task ExportInvestigationsAsync_ExcelFormat_ReturnsExcelFile()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 1 }, "excel");
            var execution = new InvestigationExecution 
            { 
                Id = 1, 
                InvestigatorId = Guid.NewGuid(),
                ResultCount = 1,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Investigator = new InvestigatorInstance { CustomName = "Test" }
            };

            _mockExecutionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationExecution, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationExecution>, IOrderedQueryable<InvestigationExecution>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationExecution> { execution });

            _mockResultRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<InvestigationResult, bool>>>()))
                .ReturnsAsync(0);

            _mockResultRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationResult, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationResult>, IOrderedQueryable<InvestigationResult>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationResult>());

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
            Assert.Contains(".xlsx", result.FileName);
        }

        [Fact]
        public async Task ExportInvestigationsAsync_NoValidInvestigations_ReturnsNull()
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 999 }, "json");

            _mockExecutionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationExecution, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationExecution>, IOrderedQueryable<InvestigationExecution>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationExecution>());

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("CSV")]
        [InlineData("EXCEL")]
        [InlineData("json")]
        [InlineData("csv")]
        [InlineData("excel")]
        public async Task ExportInvestigationsAsync_CaseInsensitiveFormat_AcceptsAllCases(string format)
        {
            // Arrange
            var request = new BulkExportRequestDto(new List<int> { 1 }, format);
            var execution = new InvestigationExecution 
            { 
                Id = 1, 
                InvestigatorId = Guid.NewGuid(),
                ResultCount = 1,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
                Investigator = new InvestigatorInstance { CustomName = "Test" }
            };

            _mockExecutionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationExecution, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationExecution>, IOrderedQueryable<InvestigationExecution>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationExecution> { execution });

            _mockResultRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<InvestigationResult, bool>>>()))
                .ReturnsAsync(0);

            _mockResultRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<InvestigationResult, bool>>>(),
                It.IsAny<Func<IQueryable<InvestigationResult>, IOrderedQueryable<InvestigationResult>>>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<InvestigationResult>());

            // Act
            var result = await _service.ExportInvestigationsAsync(request);

            // Assert
            Assert.NotNull(result);
        }
    }
}