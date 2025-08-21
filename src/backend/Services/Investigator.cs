using System;
using ea_Tracker.Models;
using ea_Tracker.Enums;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Base class for all investigators responsible for monitoring entities.
    /// Implements the template pattern to standardize lifecycle events.
    /// </summary>
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public abstract class Investigator
    {
        /// <summary>
        /// Initializes a new unique identifier for the investigator.
        /// </summary>
        public Guid Id { get; }
        // The database InvestigatorInstance Id, set by manager so notifications use persistent id
        public Guid? ExternalId { get; set; }

        // Removed IsRunning - investigations are one-shot operations

        /// <summary>
        /// Optional callback used to record investigator results.
        /// </summary>
        public Action<InvestigationResult>? Report { get; set; }
        public IInvestigationNotificationService? Notifier { get; set; }
        protected readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Investigator"/> class with a logger.
        /// </summary>
        protected Investigator(string name, ILogger? logger)
        {
            Name = name;
            Id = Guid.NewGuid();
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Investigator"/> class.
        /// </summary>
        protected Investigator(string name)
            : this(name, NullLogger.Instance)
        {
        }

        /// <summary>
        /// Gets the human readable name of the investigator.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Executes the investigation as a one-shot operation.
        /// </summary>
        public void Execute()
        {
            var notifyId = ExternalId ?? Id;
            
            // Notify start
            RecordResult($"{Name} started.");
            if (Notifier != null)
            {
                _ = Notifier.InvestigationStartedAsync(notifyId, DateTime.UtcNow);
                _ = Notifier.StatusChangedAsync(notifyId, "Running");
            }
            
            // Do the investigation work
            OnInvestigate();
            
            // Just log completion - manager will send notification after DB update
            RecordResult($"{Name} completed.");
        }

        // Removed Stop() - investigations are one-shot operations that complete naturally

        /// <summary>
        /// Executes the investigation logic.
        /// </summary>
        protected abstract void OnInvestigate();

        /// <summary>
        /// Logs a timestamped message to the console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void Log(string message)
        {
            _logger.LogInformation("[{Timestamp}] {Message}", DateTime.Now, message);
        }

        /// <summary>
        /// Records a result entry and forwards it using the <see cref="Report"/> delegate.
        /// </summary>
        /// <param name="message">The message to record.</param>
        /// <param name="payload">Optional payload for additional data.</param>
        /// <param name="severity">The severity level for the result. Defaults to Info for backwards compatibility.</param>
        protected void RecordResult(string message, string? payload = null, ResultSeverity severity = ResultSeverity.Info)
        {
            Log(message);
            var res = new InvestigationResult
            {
                ExecutionId = 0, // filled at persistence time by InvestigationManager
                Timestamp = DateTime.UtcNow,
                Severity = severity,
                Message = message ?? string.Empty,
                Payload = payload
            };
            Report?.Invoke(res);
            if (Notifier != null)
            {
                var notifyId = ExternalId ?? Id;
                _ = Notifier.NewResultAddedAsync(notifyId, res);
            }
        }
    }
}
