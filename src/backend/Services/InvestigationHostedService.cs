using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Background service that ensures the investigation infrastructure is ready.
    /// Since InvestigationManager is scoped, we use a service scope for initialization.
    /// </summary>
    public class InvestigationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InvestigationHostedService> _logger;

        private readonly IInvestigationJobQueue _queue;

        public InvestigationHostedService(
            IServiceProvider serviceProvider,
            ILogger<InvestigationHostedService> logger,
            IInvestigationJobQueue queue)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queue = queue;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Investigation background service started.");
            
            // Initialize investigation infrastructure
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<IInvestigationManager>();
            _logger.LogInformation("Investigation manager initialized successfully.");

            // Process queued start jobs
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _queue.DequeueAsync(stoppingToken);
                    using var jobScope = _serviceProvider.CreateScope();
                    var scopedManager = jobScope.ServiceProvider.GetRequiredService<IInvestigationManager>();

                    _logger.LogInformation("Processing StartInvestigator job {JobId} for {InvestigatorId}", job.JobId, job.InvestigatorId);
                    _ = scopedManager.StartInvestigatorAsync(job.InvestigatorId);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing StartInvestigator job");
                }
            }
        }
    }
}
