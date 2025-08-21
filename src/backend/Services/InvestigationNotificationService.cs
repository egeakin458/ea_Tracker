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
            var turkeyTimeOffset = TimezoneService.ConvertUtcToTurkeyDateTimeOffset(timestamp);
            _logger.LogInformation("SignalR: Sending InvestigationStarted event for {InvestigatorId} at {Timestamp} (Turkey time)", investigatorId, turkeyTimeOffset);
            return _hubContext.Clients.All.SendAsync("InvestigationStarted", new
            {
                investigatorId,
                timestamp = turkeyTimeOffset
            });
        }

        public Task InvestigationCompletedAsync(Guid investigatorId, int resultCount, DateTime timestamp)
        {
            var turkeyTimeOffset = TimezoneService.ConvertUtcToTurkeyDateTimeOffset(timestamp);
            _logger.LogInformation("SignalR: Sending InvestigationCompleted event for {InvestigatorId} with {ResultCount} results at {Timestamp} (Turkey time)", investigatorId, resultCount, turkeyTimeOffset);
            return _hubContext.Clients.All.SendAsync("InvestigationCompleted", new
            {
                investigatorId,
                resultCount,
                timestamp = turkeyTimeOffset
            });
        }

        public Task NewResultAddedAsync(Guid investigatorId, InvestigationResult? result)
        {
            if (result == null)
            {
                _logger.LogWarning("Attempted to send null result for investigator {InvestigatorId}", investigatorId);
                return Task.CompletedTask;
            }

            var turkeyTimeOffset = TimezoneService.ConvertUtcToTurkeyDateTimeOffset(result.Timestamp);
            _logger.LogInformation("SignalR: Sending NewResultAdded event for {InvestigatorId}: {Message} at {Timestamp} (Turkey time)", investigatorId, result.Message, turkeyTimeOffset);
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
                    result.Payload,
                    Timestamp = turkeyTimeOffset // Convert to DateTimeOffset for consistency
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


