using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a single execution session of an investigator instance.
    /// Tracks when the investigator was started, completed, and any results generated.
    /// </summary>
    public class InvestigationExecution
    {
        /// <summary>
        /// Gets or sets the unique identifier for this execution.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the investigator instance that was executed.
        /// </summary>
        [Required]
        public Guid InvestigatorId { get; set; }

        /// <summary>
        /// Gets or sets when this execution was started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when this execution was completed, if it has finished.
        /// Null indicates the execution is still running or was interrupted.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the current status of this execution.
        /// </summary>
        public ExecutionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the total number of results generated during this execution.
        /// Updated as results are created.
        /// </summary>
        public int ResultCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets any error message if the execution failed.
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Navigation property to the investigator instance that was executed.
        /// </summary>
        public InvestigatorInstance Investigator { get; set; } = null!;

        /// <summary>
        /// Navigation property to all results generated during this execution.
        /// </summary>
        public ICollection<InvestigationResult> Results { get; set; } = new List<InvestigationResult>();

        /// <summary>
        /// Gets the duration of this execution if it has completed.
        /// </summary>
        [NotMapped]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt - StartedAt : null;

        /// <summary>
        /// Gets a value indicating whether this execution is currently running.
        /// </summary>
        [NotMapped]
        public bool IsRunning => Status == ExecutionStatus.Running;

        /// <summary>
        /// Gets a value indicating whether this execution completed successfully.
        /// </summary>
        [NotMapped]
        public bool IsCompleted => Status == ExecutionStatus.Completed;

        /// <summary>
        /// Gets a value indicating whether this execution failed.
        /// </summary>
        [NotMapped]
        public bool IsFailed => Status == ExecutionStatus.Failed;
    }
}