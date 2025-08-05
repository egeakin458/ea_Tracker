using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a type/template of investigator that can be instantiated.
    /// This is reference data that defines what kinds of investigators are available.
    /// </summary>
    public class InvestigatorType
    {
        /// <summary>
        /// Gets or sets the unique identifier for the investigator type.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique code for this investigator type (e.g., "invoice", "waybill").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        /// <summary>
        /// Gets or sets the human-readable display name for this investigator type.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// Gets or sets an optional description of what this investigator type does.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default JSON configuration template for new instances of this type.
        /// </summary>
        public string? DefaultConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this investigator type is available for use.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets when this investigator type was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Navigation property to all instances created from this type.
        /// </summary>
        public ICollection<InvestigatorInstance> Instances { get; set; } = new List<InvestigatorInstance>();
    }
}