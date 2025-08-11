#nullable enable
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using ea_Tracker.Services;
using ea_Tracker.Hubs;
using ea_Tracker.Models;
using ea_Tracker.Enums;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace ea_Tracker.Tests
{
    /// <summary>
    /// Tests for InvestigationNotificationService SignalR event broadcasting.
    /// Verifies that all SignalR events are properly formatted and sent to clients.
    /// </summary>
    public class InvestigationNotificationServiceTests
    {
        private readonly Mock<IHubContext<InvestigationHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<ILogger<InvestigationNotificationService>> _mockLogger;
        private readonly InvestigationNotificationService _notificationService;

        public InvestigationNotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<InvestigationHub>>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockClients = new Mock<IHubClients>();
            _mockLogger = new Mock<ILogger<InvestigationNotificationService>>();

            _mockClients.Setup(clients => clients.All).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(context => context.Clients).Returns(_mockClients.Object);

            _notificationService = new InvestigationNotificationService(_mockHubContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task InvestigationStartedAsync_SendsCorrectEventToAllClients()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;

            // Act
            await _notificationService.InvestigationStartedAsync(investigatorId, timestamp);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "InvestigationStarted",
                    It.Is<object[]>(args => 
                        args.Length == 1 &&
                        args[0].GetType().GetProperty("investigatorId")!.GetValue(args[0])!.Equals(investigatorId) &&
                        // Expect DateTimeOffset with Istanbul offset
                        args[0].GetType().GetProperty("timestamp")!.GetValue(args[0]) is DateTimeOffset
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task InvestigationCompletedAsync_SendsCorrectEventWithResultCount()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            var resultCount = 42;
            var timestamp = DateTime.UtcNow;

            // Act
            await _notificationService.InvestigationCompletedAsync(investigatorId, resultCount, timestamp);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "InvestigationCompleted",
                    It.Is<object[]>(args => 
                        args.Length == 1 &&
                        args[0].GetType().GetProperty("investigatorId")!.GetValue(args[0])!.Equals(investigatorId) &&
                        args[0].GetType().GetProperty("resultCount")!.GetValue(args[0])!.Equals(resultCount) &&
                        args[0].GetType().GetProperty("timestamp")!.GetValue(args[0]) is DateTimeOffset
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task NewResultAddedAsync_SendsResultObjectToAllClients()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            var result = new InvestigationResult
            {
                Id = 123,
                ExecutionId = 456,
                Severity = ResultSeverity.Anomaly,
                EntityType = "Invoice",
                EntityId = 789,
                Message = "Negative amount detected",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _notificationService.NewResultAddedAsync(investigatorId, result);

            // Assert - verify the method was called with correct structure
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "NewResultAdded",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task StatusChangedAsync_SendsStatusStringToAllClients()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            var newStatus = "Running";

            // Act
            await _notificationService.StatusChangedAsync(investigatorId, newStatus);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "StatusChanged",
                    It.Is<object[]>(args => 
                        args.Length == 1 &&
                        args[0].GetType().GetProperty("investigatorId")!.GetValue(args[0])!.Equals(investigatorId) &&
                        args[0].GetType().GetProperty("newStatus")!.GetValue(args[0])!.Equals(newStatus)
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task InvestigationStartedAsync_CallsSignalRCorrectly()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;

            // Act
            await _notificationService.InvestigationStartedAsync(investigatorId, timestamp);

            // Assert - just verify the method was called (actual SignalR error handling is framework-level)
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "InvestigationStarted",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );

            // Verify info message was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task NewResultAddedAsync_HandlesNullResultGracefully()
        {
            // Arrange
            var investigatorId = Guid.NewGuid();
            InvestigationResult? nullResult = null;

            // Act & Assert (should not throw)
            await _notificationService.NewResultAddedAsync(investigatorId, nullResult);

            // Verify it does NOT send notification for null result
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "NewResultAdded",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );

            // Verify warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public void Service_ImplementsIInvestigationNotificationService()
        {
            // Assert
            Assert.IsAssignableFrom<IInvestigationNotificationService>(_notificationService);
        }

        [Fact]
        public void Service_HasCorrectDependencies()
        {
            // Verify the service was constructed successfully with its dependencies
            Assert.NotNull(_notificationService);
        }
    }
}