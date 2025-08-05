using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a single result or finding generated during an investigation execution.
    /// This replaces the original InvestigatorResult class with proper database design.
    /// </summary>
    public class InvestigationResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for this result.
        /// Uses bigint to handle high-volume result storage.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the execution that generated this result.
        /// </summary>
        [Required]
        public int ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets when this result was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the severity level of this result.
        /// </summary>
        public ResultSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the human-readable message describing this result.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type of business entity this result relates to, if any.
        /// Examples: "Invoice", "Waybill"
        /// </summary>
        [MaxLength(50)]
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the business entity this result relates to, if any.
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Gets or sets optional JSON payload containing additional details about this result.
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// Navigation property to the execution that generated this result.
        /// </summary>
        public InvestigationExecution Execution { get; set; } = null!;

        /// <summary>
        /// Gets a value indicating whether this result relates to a specific business entity.
        /// </summary>
        [NotMapped]
        public bool HasEntityReference => !string.IsNullOrEmpty(EntityType) && EntityId.HasValue;

        /// <summary>
        /// Gets a value indicating whether this result includes additional payload data.
        /// </summary>
        [NotMapped]
        public bool HasPayload => !string.IsNullOrEmpty(Payload);

        /// <summary>
        /// Gets a value indicating whether this result represents an anomaly or issue.
        /// </summary>
        [NotMapped]
        public bool IsAnomaly => Severity == ResultSeverity.Anomaly || Severity == ResultSeverity.Critical;
    }
}