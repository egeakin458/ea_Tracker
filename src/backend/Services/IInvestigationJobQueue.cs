using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    public record StartInvestigatorJob(Guid InvestigatorId, Guid JobId);

    public interface IInvestigationJobQueue
    {
        ValueTask EnqueueAsync(StartInvestigatorJob job, CancellationToken cancellationToken = default);
        ValueTask<StartInvestigatorJob> DequeueAsync(CancellationToken cancellationToken);
    }
}


