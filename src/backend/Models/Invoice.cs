using ea_Tracker.Enums;
using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Interface for entities that can be investigated for anomalies.
    /// Provides a common contract for investigation eligibility and tracking.
    /// Enables polymorphic investigation processing across different entity types.
    /// </summary>
    public interface IInvestigableEntity
    {
        /// <summary>
        /// Gets the unique identifier for this investigable entity.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the recipient associated with this entity.
        /// </summary>
        string? RecipientName { get; set; }

        /// <summary>
        /// Gets or sets when this entity record was created in the system.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this entity record was last updated.
        /// </summary>
        DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether any investigations have detected anomalies in this entity.
        /// </summary>
        bool HasAnomalies { get; set; }

        /// <summary>
        /// Gets or sets when this entity was last investigated, if ever.
        /// </summary>
        DateTime? LastInvestigatedAt { get; set; }
    }

    /// <summary>
    /// Extension methods for IInvestigableEntity providing common business operations.
    /// These methods encapsulate business logic for investigation eligibility and marking.
    /// </summary>
    public static class InvestigableEntityExtensions
    {
        /// <summary>
        /// Determines if this entity is eligible for investigation based on business rules.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="investigationCooldownHours">Minimum hours between investigations (default: 1).</param>
        /// <returns>True if the entity should be investigated, false otherwise.</returns>
        public static bool IsEligibleForInvestigation(this IInvestigableEntity entity, int investigationCooldownHours = 1)
        {
            if (entity == null)
                return false;

            // Always investigate entities that have never been investigated
            if (!entity.LastInvestigatedAt.HasValue)
                return true;

            // Apply cooldown period to prevent excessive re-investigation
            var cooldownThreshold = DateTime.UtcNow.AddHours(-investigationCooldownHours);
            return entity.LastInvestigatedAt.Value < cooldownThreshold;
        }

        /// <summary>
        /// Marks the entity as investigated with the specified results.
        /// </summary>
        /// <param name="entity">The entity to mark.</param>
        /// <param name="hasAnomalies">Whether anomalies were detected during investigation.</param>
        /// <param name="investigatedAt">When the investigation occurred (default: current UTC time).</param>
        public static void MarkAsInvestigated(this IInvestigableEntity entity, bool hasAnomalies, DateTime? investigatedAt = null)
        {
            if (entity == null)
                return;

            entity.HasAnomalies = hasAnomalies;
            entity.LastInvestigatedAt = investigatedAt ?? DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the number of days since the entity was last investigated.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>Days since last investigation, or null if never investigated.</returns>
        public static int? DaysSinceLastInvestigation(this IInvestigableEntity entity)
        {
            if (entity?.LastInvestigatedAt == null)
                return null;

            return (int)(DateTime.UtcNow - entity.LastInvestigatedAt.Value).TotalDays;
        }

        /// <summary>
        /// Determines if the entity requires priority investigation based on age and anomaly history.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="maxDaysWithoutInvestigation">Maximum days without investigation before priority (default: 7).</param>
        /// <returns>True if priority investigation is recommended.</returns>
        public static bool RequiresPriorityInvestigation(this IInvestigableEntity entity, int maxDaysWithoutInvestigation = 7)
        {
            if (entity == null)
                return false;

            // Priority if never investigated and older than 1 day
            if (!entity.LastInvestigatedAt.HasValue)
            {
                var daysSinceCreated = (DateTime.UtcNow - entity.CreatedAt).TotalDays;
                return daysSinceCreated >= 1;
            }

            // Priority if too much time has passed since last investigation
            var daysSinceInvestigation = DaysSinceLastInvestigation(entity);
            return daysSinceInvestigation >= maxDaysWithoutInvestigation;
        }

        /// <summary>
        /// Gets a summary description of the entity's investigation status.
        /// </summary>
        /// <param name="entity">The entity to describe.</param>
        /// <returns>Human-readable investigation status description.</returns>
        public static string GetInvestigationStatusDescription(this IInvestigableEntity entity)
        {
            if (entity == null)
                return "Unknown entity";

            if (!entity.LastInvestigatedAt.HasValue)
                return "Never investigated";

            var days = DaysSinceLastInvestigation(entity);
            var anomalyStatus = entity.HasAnomalies ? "with anomalies" : "clean";
            
            return days switch
            {
                0 => $"Investigated today ({anomalyStatus})",
                1 => $"Investigated yesterday ({anomalyStatus})",
                _ => $"Investigated {days} days ago ({anomalyStatus})"
            };
        }
    }
    /// <summary>
    /// Represents an invoice issued to a recipient.
    /// Enhanced with audit fields and investigation tracking.
    /// Implements IInvestigableEntity for polymorphic investigation processing.
    /// </summary>
    public class Invoice : IInvestigableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the invoice.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the recipient.
        /// </summary>
        [MaxLength(200)]
        public string? RecipientName { get; set; }

        /// <summary>
        /// Gets or sets the total amount of the invoice.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the date the invoice was issued.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the total tax applied to the invoice.
        /// </summary>
        public decimal TotalTax { get; set; }

        /// <summary>
        /// Gets or sets the type of the invoice.
        /// </summary>
        public InvoiceType InvoiceType { get; set; }

        /// <summary>
        /// Gets or sets when this invoice record was created in the system.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when this invoice record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether any investigations have detected anomalies in this invoice.
        /// </summary>
        public bool HasAnomalies { get; set; } = false;

        /// <summary>
        /// Gets or sets when this invoice was last investigated, if ever.
        /// </summary>
        public DateTime? LastInvestigatedAt { get; set; }
    }
}
