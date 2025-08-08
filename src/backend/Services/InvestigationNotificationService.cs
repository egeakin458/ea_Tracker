using ea_Tracker.Hubs;
using ea_Tracker.Models;
using Microsoft.AspNetCore.SignalR;

namespace ea_Tracker.Services
{
    public class InvestigationNotificationService : IInvestigationNotificationService
    {
        private readonly IHubContext<InvestigationHub> _hubContext;

        public InvestigationNotificationService(IHubContext<InvestigationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task InvestigationStartedAsync(Guid investigatorId, DateTime timestamp)
        {
            return _hubContext.Clients.All.SendAsync("InvestigationStarted", new
            {
                investigatorId,
                timestamp
            });
        }

        public Task InvestigationCompletedAsync(Guid investigatorId, int resultCount, DateTime timestamp)
        {
            return _hubContext.Clients.All.SendAsync("InvestigationCompleted", new
            {
                investigatorId,
                resultCount,
                timestamp
            });
        }

        public Task NewResultAddedAsync(Guid investigatorId, InvestigationResult result)
        {
            return _hubContext.Clients.All.SendAsync("NewResultAdded", new
            {
                investigatorId,
                result
            });
        }

        public Task StatusChangedAsync(Guid investigatorId, string newStatus)
        {
            return _hubContext.Clients.All.SendAsync("StatusChanged", new
            {
                investigatorId,
                newStatus
            });
        }
    }
}


