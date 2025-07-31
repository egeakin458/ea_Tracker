namespace ea_Tracker.Services
{
    /// <summary>
    /// Base class for all investigators responsible for monitoring entities.
    /// Implements the template pattern to standardize lifecycle events.
    /// </summary>
    public abstract class Investigator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Investigator"/> class.
        /// </summary>
        /// <param name="name">The human readable name of the investigator.</param>
        protected Investigator(string name)
        {
            Name = name;
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
            Log($"{Name} started.");
            try
            {
                OnStart();
            }
            finally
            {
                Log($"{Name} finished.");
            }
        }

        /// <summary>
        /// Stops the investigator by invoking <see cref="OnStop"/> and logging lifecycle events.
        /// </summary>
        public void Stop()
        {
            Log($"{Name} stopped.");
            OnStop();
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
    }
}

