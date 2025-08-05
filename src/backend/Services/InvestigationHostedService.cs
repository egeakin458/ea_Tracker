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

        public InvestigationHostedService(
            IServiceProvider serviceProvider,
            ILogger<InvestigationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Investigation background service started.");
            
            // Initialize investigation infrastructure
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<InvestigationManager>();
            _logger.LogInformation("Investigation manager initialized successfully.");
            
            // Keep running until shutdown; actual investigator triggers are via API calls
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
