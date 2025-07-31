using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents an invoice issued to a recipient.
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// Gets or sets the unique identifier for the invoice.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the recipient.
        /// </summary>
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
    }
}
