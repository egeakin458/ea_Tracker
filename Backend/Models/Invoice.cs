using ea_Tracker.Enums;
using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents an invoice issued to a recipient.
    /// Enhanced with audit fields and investigation tracking.
    /// </summary>
    public class Invoice
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
