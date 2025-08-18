using ea_Tracker.Enums;
using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a waybill for shipped goods.
    /// Enhanced with audit fields and investigation tracking.
    /// Implements IInvestigableEntity for polymorphic investigation processing.
    /// </summary>
    public class Waybill : IInvestigableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the waybill.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the recipient.
        /// </summary>
        [MaxLength(200)]
        public string? RecipientName { get; set; }

        /// <summary>
        /// Gets or sets the date the goods were issued.
        /// </summary>
        public DateTime GoodsIssueDate { get; set; }

        /// <summary>
        /// Gets or sets the type of the waybill.
        /// </summary>
        public WaybillType WaybillType { get; set; }

        /// <summary>
        /// Gets or sets the items shipped in this waybill.
        /// </summary>
        [MaxLength(1000)]
        public string? ShippedItems { get; set; }

        /// <summary>
        /// Gets or sets when this waybill record was created in the system.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this waybill record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether any investigations have detected anomalies in this waybill.
        /// </summary>
        public bool HasAnomalies { get; set; } = false;

        /// <summary>
        /// Gets or sets when this waybill was last investigated, if ever.
        /// </summary>
        public DateTime? LastInvestigatedAt { get; set; }

        /// <summary>
        /// Gets or sets the due date for delivery of this waybill.
        /// Used by investigation algorithms to detect overdue and expiring deliveries.
        /// Nullable to support existing data without breaking changes.
        /// </summary>
        public DateTime? DueDate { get; set; }
    }
}
