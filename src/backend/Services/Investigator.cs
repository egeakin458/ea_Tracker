using System;
using ea_Tracker.Models;

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

        /// <summary>
        /// Gets a value indicating whether the investigator is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Optional callback used to record investigator results.
        /// </summary>
        public Action<InvestigatorResult>? Report { get; set; }
        public IInvestigationNotificationService? Notifier { get; set; }
        private readonly ILogger _logger;

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
        /// Starts the investigator by invoking <see cref="OnStart"/> and logging lifecycle events.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            RecordResult($"{Name} started.");
            if (Notifier != null)
            {
                var notifyId = ExternalId ?? Id;
                _ = Notifier.InvestigationStartedAsync(notifyId, DateTime.UtcNow);
                _ = Notifier.StatusChangedAsync(notifyId, "Running");
            }
            OnStart();
        }

        /// <summary>
        /// Stops the investigator by invoking <see cref="OnStop"/> and logging lifecycle events.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            OnStop();
            IsRunning = false;
            RecordResult($"{Name} stopped.");
            if (Notifier != null)
            {
                var notifyId = ExternalId ?? Id;
                _ = Notifier.StatusChangedAsync(notifyId, "Stopped");
            }
        }

        /// <summary>
        /// Executes investigator specific startup logic.
        /// </summary>
        protected abstract void OnStart();

        /// <summary>
        /// Executes investigator specific shutdown logic.
        /// </summary>
        protected abstract void OnStop();

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
        protected void RecordResult(string message, string? payload = null)
        {
            Log(message);
            var res = new InvestigatorResult
            {
                InvestigatorId = Id,
                Timestamp = DateTime.UtcNow,
                Message = message,
                Payload = payload
            };
            Report?.Invoke(res);
            if (Notifier != null)
            {
                var notifyId = ExternalId ?? Id;
                _ = Notifier.NewResultAddedAsync(notifyId, new Models.InvestigationResult
                {
                    ExecutionId = 0, // filled at persistence time; clients don't need exact value
                    Timestamp = res.Timestamp,
                    Message = res.Message ?? string.Empty,
                    Payload = res.Payload
                });
            }
        }
    }
}
