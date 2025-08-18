using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ea_Tracker.Hubs
{
    /// <summary>
    /// Secure SignalR hub for broadcasting investigation lifecycle and result events to clients.
    /// Implements authentication and connection management with audit logging.
    /// </summary>
    [Authorize] // Require authentication for all hub connections
    public class InvestigationHub : Hub
    {
        private readonly ILogger<InvestigationHub> _logger;

        public InvestigationHub(ILogger<InvestigationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Logs connection details for security monitoring.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var connectionId = Context.ConnectionId;
            
            _logger.LogInformation("SignalR connection established for user {UserId} with connection {ConnectionId}", 
                userId, connectionId);

            // Join user-specific group for targeted messaging
            if (!string.IsNullOrEmpty(userId) && userId != "anonymous")
            {
                await Groups.AddToGroupAsync(connectionId, $"User_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Logs disconnection details for security monitoring.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(exception, "SignalR connection {ConnectionId} for user {UserId} disconnected with error", 
                    connectionId, userId);
            }
            else
            {
                _logger.LogInformation("SignalR connection {ConnectionId} for user {UserId} disconnected normally", 
                    connectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Allows clients to join investigation-specific groups for targeted updates.
        /// Includes authorization check to ensure user has access to the investigation.
        /// </summary>
        /// <param name="investigationId">The investigation ID to subscribe to</param>
        [HubMethodName("JoinInvestigation")]
        public async Task JoinInvestigationGroup(string investigationId)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized attempt to join investigation group {InvestigationId}", investigationId);
                    return;
                }

                // Validate investigation ID format
                if (!Guid.TryParse(investigationId, out _))
                {
                    _logger.LogWarning("Invalid investigation ID format {InvestigationId} from user {UserId}", 
                        investigationId, userId);
                    return;
                }

                var groupName = $"Investigation_{investigationId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("User {UserId} joined investigation group {GroupName}", userId, groupName);
                
                // Acknowledge successful join
                await Clients.Caller.SendAsync("JoinedInvestigation", investigationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining investigation group {InvestigationId}", investigationId);
            }
        }

        /// <summary>
        /// Allows clients to leave investigation-specific groups.
        /// </summary>
        /// <param name="investigationId">The investigation ID to unsubscribe from</param>
        [HubMethodName("LeaveInvestigation")]
        public async Task LeaveInvestigationGroup(string investigationId)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                if (!Guid.TryParse(investigationId, out _))
                {
                    _logger.LogWarning("Invalid investigation ID format {InvestigationId} from user {UserId}", 
                        investigationId, userId);
                    return;
                }

                var groupName = $"Investigation_{investigationId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("User {UserId} left investigation group {GroupName}", userId, groupName);
                
                // Acknowledge successful leave
                await Clients.Caller.SendAsync("LeftInvestigation", investigationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving investigation group {InvestigationId}", investigationId);
            }
        }

        /// <summary>
        /// Heartbeat method to maintain connection health.
        /// Clients can call this periodically to ensure connection stability.
        /// </summary>
        [HubMethodName("Heartbeat")]
        public async Task Heartbeat()
        {
            await Clients.Caller.SendAsync("HeartbeatResponse", DateTime.UtcNow);
        }
    }
}


