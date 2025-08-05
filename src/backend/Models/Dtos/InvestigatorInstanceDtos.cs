using ea_Tracker.Enums;
using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// DTO for creating a new investigator instance.
    /// </summary>
    public class CreateInvestigatorInstanceDto
    {
        /// <summary>
        /// The investigator type code (e.g., "invoice", "waybill").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TypeCode { get; set; } = null!;

        /// <summary>
        /// Optional custom name for this investigator instance.
        /// </summary>
        [MaxLength(200)]
        public string? CustomName { get; set; }

        /// <summary>
        /// Optional custom JSON configuration that overrides the type's default configuration.
        /// </summary>
        public string? CustomConfiguration { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing investigator instance.
    /// </summary>
    public class UpdateInvestigatorInstanceDto
    {
        /// <summary>
        /// Optional custom name for this investigator instance.
        /// </summary>
        [MaxLength(200)]
        public string? CustomName { get; set; }

        /// <summary>
        /// Whether this investigator instance is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional custom JSON configuration that overrides the type's default configuration.
        /// </summary>
        public string? CustomConfiguration { get; set; }
    }

    /// <summary>
    /// DTO for investigator instance responses.
    /// </summary>
    public class InvestigatorInstanceResponseDto
    {
        /// <summary>
        /// The unique identifier for this investigator instance.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The investigator type information.
        /// </summary>
        public InvestigatorTypeDto Type { get; set; } = null!;

        /// <summary>
        /// The display name for this investigator instance.
        /// </summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// Optional custom name for this investigator instance.
        /// </summary>
        public string? CustomName { get; set; }

        /// <summary>
        /// When this investigator instance was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this investigator was last executed, if ever.
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }

        /// <summary>
        /// Whether this investigator instance is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The current status of this investigator instance.
        /// </summary>
        public InvestigatorStatus Status { get; set; }

        /// <summary>
        /// The total number of results across all executions.
        /// </summary>
        public int TotalResultCount { get; set; }

        /// <summary>
        /// Optional custom JSON configuration.
        /// </summary>
        public string? CustomConfiguration { get; set; }
    }

    /// <summary>
    /// DTO for investigator type information.
    /// </summary>
    public class InvestigatorTypeDto
    {
        /// <summary>
        /// The unique identifier for this investigator type.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique code for this investigator type.
        /// </summary>
        public string Code { get; set; } = null!;

        /// <summary>
        /// The human-readable display name.
        /// </summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// Description of what this investigator type does.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Default JSON configuration template.
        /// </summary>
        public string? DefaultConfiguration { get; set; }

        /// <summary>
        /// Whether this investigator type is available for use.
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for investigator summary information.
    /// </summary>
    public class InvestigatorSummaryDto
    {
        /// <summary>
        /// Total number of investigator instances.
        /// </summary>
        public int TotalInvestigators { get; set; }

        /// <summary>
        /// Number of active investigator instances.
        /// </summary>
        public int ActiveInvestigators { get; set; }

        /// <summary>
        /// Number of currently running investigator instances.
        /// </summary>
        public int RunningInvestigators { get; set; }

        /// <summary>
        /// Total number of executions across all investigators.
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Total number of results across all investigators.
        /// </summary>
        public long TotalResults { get; set; }
    }
}