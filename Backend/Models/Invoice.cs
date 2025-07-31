using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents an invoice issued to a recipient.
    /// </summary>
    public class Invoice
    {
        public int Id { get; set; }
        public string? RecipientName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime IssueDate { get; set; }
        public decimal TotalTax { get; set; }
        public InvoiceType InvoiceType { get; set; }
    }
}
