using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Background service that keeps the InvestigationManager alive for the app's lifetime.
    /// </summary>
    public class InvestigationHostedService : BackgroundService
    {
        private readonly InvestigationManager _manager;
        private readonly ILogger<InvestigationHostedService> _logger;

        public InvestigationHostedService(
            InvestigationManager manager,
            ILogger<InvestigationHostedService> logger)
        {
            _manager = manager;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Investigation background service started.");
            // Keep running until shutdown; actual investigator triggers are via API calls
            return Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
