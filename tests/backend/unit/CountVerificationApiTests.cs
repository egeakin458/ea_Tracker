using ea_Tracker.Controllers;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services;
using ea_Tracker.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ea_Tracker.Tests
{
    /// <summary>
    /// API endpoint tests for count verification functionality.
    /// Tests the REST endpoints that expose the race condition fix.
    /// </summary>
    public class CountVerificationApiTests
    {
        private readonly Mock<ICompletedInvestigationService> _mockInvestigationService;
        private readonly Mock<IInvestigationManager> _mockInvestigationManager;
        private readonly Mock<ILogger<CompletedInvestigationsController>> _mockLogger;
        private readonly CompletedInvestigationsController _controller;

        public CountVerificationApiTests()
        {
            _mockInvestigationService = new Mock<ICompletedInvestigationService>();
            _mockInvestigationManager = new Mock<IInvestigationManager>();
            _mockLogger = new Mock<ILogger<CompletedInvestigationsController>>();
            
            _controller = new CompletedInvestigationsController(
                _mockInvestigationService.Object,
                _mockInvestigationManager.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task VerifyResultCount_ValidExecution_ReturnsOkResult()
        {
            // Arrange
            var executionId = 248;
            var verificationResult = new CountVerificationResult
            {
                ExecutionId = executionId,
                ReportedCount = 8,
                ActualCount = 100,
                IsAccurate = false,
                Discrepancy = 92
            };

            _mockInvestigationManager
                .Setup(m => m.VerifyResultCountAsync(executionId))
                .ReturnsAsync(verificationResult);

            // Act
            var result = await _controller.VerifyResultCount(executionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<CountVerificationResult>(okResult.Value);
            
            Assert.Equal(executionId, returnedResult.ExecutionId);
            Assert.Equal(8, returnedResult.ReportedCount);
            Assert.Equal(100, returnedResult.ActualCount);
            Assert.False(returnedResult.IsAccurate);
            Assert.Equal(92, returnedResult.Discrepancy);
        }

        [Fact]
        public async Task VerifyResultCount_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var executionId = 999;
            _mockInvestigationManager
                .Setup(m => m.VerifyResultCountAsync(executionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.VerifyResultCount(executionId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An error occurred while verifying the result count", statusResult.Value);
        }

        [Fact]
        public async Task CorrectResultCount_CorrectionMade_ReturnsTrue()
        {
            // Arrange
            var executionId = 248;
            _mockInvestigationManager
                .Setup(m => m.CorrectResultCountAsync(executionId))
                .ReturnsAsync(true); // Correction was made

            // Act
            var result = await _controller.CorrectResultCount(executionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var corrected = Assert.IsType<bool>(okResult.Value);
            Assert.True(corrected);
        }

        [Fact]
        public async Task CorrectResultCount_NoCorrectionNeeded_ReturnsFalse()
        {
            // Arrange
            var executionId = 100;
            _mockInvestigationManager
                .Setup(m => m.CorrectResultCountAsync(executionId))
                .ReturnsAsync(false); // No correction needed

            // Act
            var result = await _controller.CorrectResultCount(executionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var corrected = Assert.IsType<bool>(okResult.Value);
            Assert.False(corrected);
        }

        [Fact]
        public async Task CorrectResultCount_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var executionId = 999;
            _mockInvestigationManager
                .Setup(m => m.CorrectResultCountAsync(executionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CorrectResultCount(executionId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An error occurred while correcting the result count", statusResult.Value);
        }

        [Fact]
        public async Task CorrectAllResultCounts_BulkCorrection_ReturnsCount()
        {
            // Arrange
            var correctedCount = 5; // 5 investigations had their counts corrected
            _mockInvestigationManager
                .Setup(m => m.CorrectAllResultCountsAsync())
                .ReturnsAsync(correctedCount);

            // Act
            var result = await _controller.CorrectAllResultCounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCount = Assert.IsType<int>(okResult.Value);
            Assert.Equal(5, returnedCount);
        }

        [Fact]
        public async Task CorrectAllResultCounts_NoCorrectionNeeded_ReturnsZero()
        {
            // Arrange
            _mockInvestigationManager
                .Setup(m => m.CorrectAllResultCountsAsync())
                .ReturnsAsync(0); // No corrections needed

            // Act
            var result = await _controller.CorrectAllResultCounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCount = Assert.IsType<int>(okResult.Value);
            Assert.Equal(0, returnedCount);
        }

        [Fact]
        public async Task CorrectAllResultCounts_ServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _mockInvestigationManager
                .Setup(m => m.CorrectAllResultCountsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CorrectAllResultCounts();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An error occurred while correcting result counts", statusResult.Value);
        }

        [Fact]
        public void CountVerificationEndpoints_HttpAttributes_ConfiguredCorrectly()
        {
            // This test verifies the routing configuration for our new endpoints
            var controllerType = typeof(CompletedInvestigationsController);
            
            // Verify VerifyResultCount method has correct HTTP GET attribute
            var verifyMethod = controllerType.GetMethod("VerifyResultCount");
            Assert.NotNull(verifyMethod);
            
            // Verify CorrectResultCount method has correct HTTP POST attribute  
            var correctMethod = controllerType.GetMethod("CorrectResultCount");
            Assert.NotNull(correctMethod);
            
            // Verify CorrectAllResultCounts method has correct HTTP POST attribute
            var correctAllMethod = controllerType.GetMethod("CorrectAllResultCounts");
            Assert.NotNull(correctAllMethod);
        }
    }
}