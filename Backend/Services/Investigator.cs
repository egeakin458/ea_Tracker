namespace ea_Tracker.Services
{
    /// <summary>
    /// Base class for all investigators responsible for monitoring entities.
    /// </summary>
    public abstract class Investigator
    {
        /// <summary>
        /// Starts the investigator.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stops the investigator.
        /// </summary>
        public abstract void Stop();

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

