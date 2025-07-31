using System;
using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Base class for all investigators responsible for monitoring entities.
    /// Implements the template pattern to standardize lifecycle events.
    /// </summary>
    public abstract class Investigator
    {
        /// <summary>
        /// Initializes a new unique identifier for the investigator.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets a value indicating whether the investigator is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Optional callback used to record investigator results.
        /// </summary>
        public Action<InvestigatorResult>? Report { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Investigator"/> class.
        /// </summary>
        /// <param name="name">The human readable name of the investigator.</param>
        protected Investigator(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
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
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }

        /// <summary>
        /// Records a result entry and forwards it using the <see cref="Report"/> delegate.
        /// </summary>
        /// <param name="message">The message to record.</param>
        /// <param name="payload">Optional payload for additional data.</param>
        protected void RecordResult(string message, string? payload = null)
        {
            Log(message);
            Report?.Invoke(new InvestigatorResult
            {
                InvestigatorId = Id,
                Timestamp = DateTime.UtcNow,
                Message = message,
                Payload = payload
            });
        }
    }
}

