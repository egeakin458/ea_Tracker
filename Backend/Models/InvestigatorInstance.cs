using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents an active instance of an investigator that can be started, stopped, and configured.
    /// This is the database entity that stores investigator state and configuration.
    /// </summary>
    public class InvestigatorInstance
    {
        /// <summary>
        /// Gets or sets the unique identifier for this investigator instance.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the investigator type this instance is based on.
        /// </summary>
        [Required]
        public int TypeId { get; set; }

        /// <summary>
        /// Gets or sets an optional custom name for this investigator instance.
        /// If null, will use the type's display name.
        /// </summary>
        [MaxLength(200)]
        public string? CustomName { get; set; }

        /// <summary>
        /// Gets or sets when this investigator instance was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this investigator was last executed, if ever.
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this investigator instance is active.
        /// Inactive investigators cannot be started and are hidden from normal operations.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets custom JSON configuration that overrides the type's default configuration.
        /// </summary>
        public string? CustomConfiguration { get; set; }

        /// <summary>
        /// Navigation property to the investigator type this instance is based on.
        /// </summary>
        public InvestigatorType Type { get; set; } = null!;

        /// <summary>
        /// Navigation property to all executions of this investigator instance.
        /// </summary>
        public ICollection<InvestigationExecution> Executions { get; set; } = new List<InvestigationExecution>();

        /// <summary>
        /// Gets the display name for this investigator instance.
        /// Returns custom name if set, otherwise the type's display name.
        /// </summary>
        [NotMapped]
        public string DisplayName => CustomName ?? Type?.DisplayName ?? "Unnamed Investigator";

        /// <summary>
        /// Gets the current status of this investigator instance based on its active state and executions.
        /// </summary>
        [NotMapped]
        public InvestigatorStatus Status
        {
            get
            {
                if (!IsActive) return InvestigatorStatus.Inactive;
                
                var runningExecution = Executions?.FirstOrDefault(e => e.Status == ExecutionStatus.Running);
                if (runningExecution != null) return InvestigatorStatus.Running;
                
                var failedExecution = Executions?.OrderByDescending(e => e.StartedAt)
                    .FirstOrDefault(e => e.Status == ExecutionStatus.Failed);
                if (failedExecution != null) return InvestigatorStatus.Failed;
                
                return InvestigatorStatus.Stopped;
            }
        }

        /// <summary>
        /// Gets the total number of results across all executions of this investigator.
        /// </summary>
        [NotMapped]
        public int TotalResultCount => Executions?.Sum(e => e.ResultCount) ?? 0;
    }
}