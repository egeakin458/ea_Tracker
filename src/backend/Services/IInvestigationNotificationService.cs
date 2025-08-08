using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    public interface IInvestigationNotificationService
    {
        Task InvestigationStartedAsync(Guid investigatorId, DateTime timestamp);
        Task InvestigationCompletedAsync(Guid investigatorId, int resultCount, DateTime timestamp);
        Task NewResultAddedAsync(Guid investigatorId, InvestigationResult result);
        Task StatusChangedAsync(Guid investigatorId, string newStatus);
    }
}


