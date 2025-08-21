using ea_Tracker.Hubs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ea_Tracker.Tests
{
    /// <summary>
    /// Tests for InvestigationHub SignalR functionality.
    /// Tests basic hub instantiation and structure.
    /// </summary>
    public class InvestigationHubTests
    {
        [Fact]
        public void Hub_CanBeInstantiated()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<InvestigationHub>>();

            // Act
            var hub = new InvestigationHub(mockLogger.Object);

            // Assert
            Assert.NotNull(hub);
        }

        [Fact]
        public void Hub_ExtendsHubBaseClass()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<InvestigationHub>>();

            // Act
            var hub = new InvestigationHub(mockLogger.Object);

            // Assert
            Assert.IsAssignableFrom<Microsoft.AspNetCore.SignalR.Hub>(hub);
        }
    }
}