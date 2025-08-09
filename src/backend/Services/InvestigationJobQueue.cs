using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    public class InvestigationJobQueue : IInvestigationJobQueue
    {
        private readonly Channel<StartInvestigatorJob> _channel;

        public InvestigationJobQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _channel = Channel.CreateBounded<StartInvestigatorJob>(options);
        }

        public async ValueTask EnqueueAsync(StartInvestigatorJob job, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public async ValueTask<StartInvestigatorJob> DequeueAsync(CancellationToken cancellationToken)
        {
            var job = await _channel.Reader.ReadAsync(cancellationToken);
            return job;
        }
    }
}


