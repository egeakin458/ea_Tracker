using ea_Tracker.Hubs;
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
            // Arrange & Act
            var hub = new InvestigationHub();

            // Assert
            Assert.NotNull(hub);
        }

        [Fact]
        public void Hub_ExtendsHubBaseClass()
        {
            // Arrange & Act
            var hub = new InvestigationHub();

            // Assert
            Assert.IsAssignableFrom<Microsoft.AspNetCore.SignalR.Hub>(hub);
        }
    }
}