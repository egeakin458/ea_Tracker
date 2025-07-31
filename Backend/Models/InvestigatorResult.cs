using System;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a log entry produced by an investigator.
    /// </summary>
    public class InvestigatorResult
    {
        /// <summary>
        /// Gets or sets the identifier of the investigator that generated the entry.
        /// </summary>
        public Guid InvestigatorId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a message describing the event.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets optional payload data for the entry.
        /// </summary>
        public string? Payload { get; set; }
    }
}

