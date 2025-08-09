using ea_Tracker.Hubs;
using ea_Tracker.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    public class InvestigationNotificationService : IInvestigationNotificationService
    {
        private readonly IHubContext<InvestigationHub> _hubContext;
        private readonly ILogger<InvestigationNotificationService> _logger;

        public InvestigationNotificationService(IHubContext<InvestigationHub> hubContext, ILogger<InvestigationNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public Task InvestigationStartedAsync(Guid investigatorId, DateTime timestamp)
        {
            _logger.LogInformation("SignalR: Sending InvestigationStarted event for {InvestigatorId} at {Timestamp}", investigatorId, timestamp);
            return _hubContext.Clients.All.SendAsync("InvestigationStarted", new
            {
                investigatorId,
                timestamp
            });
        }

        public Task InvestigationCompletedAsync(Guid investigatorId, int resultCount, DateTime timestamp)
        {
            _logger.LogInformation("SignalR: Sending InvestigationCompleted event for {InvestigatorId} with {ResultCount} results at {Timestamp}", investigatorId, resultCount, timestamp);
            return _hubContext.Clients.All.SendAsync("InvestigationCompleted", new
            {
                investigatorId,
                resultCount,
                timestamp
            });
        }

        public Task NewResultAddedAsync(Guid investigatorId, InvestigationResult result)
        {
            _logger.LogInformation("SignalR: Sending NewResultAdded event for {InvestigatorId}: {Message}", investigatorId, result.Message);
            return _hubContext.Clients.All.SendAsync("NewResultAdded", new
            {
                investigatorId,
                result = new
                {
                    result.Id,
                    result.ExecutionId,
                    result.EntityType,
                    result.EntityId,
                    result.Message,
                    result.Severity,
                    result.Timestamp
                }
            });
        }

        public Task StatusChangedAsync(Guid investigatorId, string newStatus)
        {
            _logger.LogInformation("SignalR: Sending StatusChanged event for {InvestigatorId} to {NewStatus}", investigatorId, newStatus);
            return _hubContext.Clients.All.SendAsync("StatusChanged", new
            {
                investigatorId,
                newStatus
            });
        }
    }
}


