using ea_Tracker.Enums;
using ea_Tracker.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// DTO for creating a new invoice.
    /// </summary>
    public class CreateInvoiceDto
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        [MaxLength(200)]
        [SanitizedString(allowHtml: false, maxLength: 200)]
        [SqlSafe]
        public string? RecipientName { get; set; }

        /// <summary>
        /// The total amount of the invoice.
        /// </summary>
        [Required]
        [DecimalRange(-999999999.99, 999999999.99)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// The date the invoice was issued.
        /// </summary>
        [Required]
        [BusinessDateRange(maxYearsPast: 10)]
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// The total tax applied to the invoice.
        /// </summary>
        [Required]
        [DecimalRange(0, 999999999.99)]
        public decimal TotalTax { get; set; }

        /// <summary>
        /// The type of the invoice.
        /// </summary>
        [Required]
        public InvoiceType InvoiceType { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing invoice.
    /// </summary>
    public class UpdateInvoiceDto
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        [MaxLength(200)]
        [SanitizedString(allowHtml: false, maxLength: 200)]
        [SqlSafe]
        public string? RecipientName { get; set; }

        /// <summary>
        /// The total amount of the invoice.
        /// </summary>
        [Required]
        [DecimalRange(-999999999.99, 999999999.99)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// The date the invoice was issued.
        /// </summary>
        [Required]
        [BusinessDateRange(maxYearsPast: 10)]
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// The total tax applied to the invoice.
        /// </summary>
        [Required]
        [DecimalRange(0, 999999999.99)]
        public decimal TotalTax { get; set; }

        /// <summary>
        /// The type of the invoice.
        /// </summary>
        [Required]
        public InvoiceType InvoiceType { get; set; }
    }

    /// <summary>
    /// DTO for invoice responses.
    /// </summary>
    public class InvoiceResponseDto
    {
        /// <summary>
        /// The unique identifier for the invoice.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the recipient.
        /// </summary>
        public string? RecipientName { get; set; }

        /// <summary>
        /// The total amount of the invoice.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// The date the invoice was issued.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// The total tax applied to the invoice.
        /// </summary>
        public decimal TotalTax { get; set; }

        /// <summary>
        /// The type of the invoice.
        /// </summary>
        public InvoiceType InvoiceType { get; set; }

        /// <summary>
        /// When this invoice record was created in the system.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this invoice record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether any investigations have detected anomalies in this invoice.
        /// </summary>
        public bool HasAnomalies { get; set; }

        /// <summary>
        /// When this invoice was last investigated, if ever.
        /// </summary>
        public DateTime? LastInvestigatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new waybill.
    /// </summary>
    public class CreateWaybillDto
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        [MaxLength(200)]
        [SanitizedString(allowHtml: false, maxLength: 200)]
        [SqlSafe]
        public string? RecipientName { get; set; }

        /// <summary>
        /// The date the goods were issued.
        /// </summary>
        [Required]
        [BusinessDateRange(maxYearsPast: 10)]
        public DateTime GoodsIssueDate { get; set; }

        /// <summary>
        /// The type of the waybill.
        /// </summary>
        [Required]
        public WaybillType WaybillType { get; set; }

        /// <summary>
        /// The items shipped in this waybill.
        /// </summary>
        [MaxLength(1000)]
        [SanitizedString(allowHtml: false, maxLength: 1000)]
        [SqlSafe]
        public string? ShippedItems { get; set; }

        /// <summary>
        /// The due date for delivery of this waybill.
        /// Optional field for new delivery tracking algorithms.
        /// </summary>
        public DateTime? DueDate { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing waybill.
    /// </summary>
    public class UpdateWaybillDto
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        [MaxLength(200)]
        [SanitizedString(allowHtml: false, maxLength: 200)]
        [SqlSafe]
        public string? RecipientName { get; set; }

        /// <summary>
        /// The date the goods were issued.
        /// </summary>
        [Required]
        [BusinessDateRange(maxYearsPast: 10)]
        public DateTime GoodsIssueDate { get; set; }

        /// <summary>
        /// The type of the waybill.
        /// </summary>
        [Required]
        public WaybillType WaybillType { get; set; }

        /// <summary>
        /// The items shipped in this waybill.
        /// </summary>
        [MaxLength(1000)]
        [SanitizedString(allowHtml: false, maxLength: 1000)]
        [SqlSafe]
        public string? ShippedItems { get; set; }

        /// <summary>
        /// The due date for delivery of this waybill.
        /// Optional field for new delivery tracking algorithms.
        /// </summary>
        public DateTime? DueDate { get; set; }
    }

    /// <summary>
    /// DTO for waybill responses.
    /// </summary>
    public class WaybillResponseDto
    {
        /// <summary>
        /// The unique identifier for the waybill.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the recipient.
        /// </summary>
        public string? RecipientName { get; set; }

        /// <summary>
        /// The date the goods were issued.
        /// </summary>
        public DateTime GoodsIssueDate { get; set; }

        /// <summary>
        /// The type of the waybill.
        /// </summary>
        public WaybillType WaybillType { get; set; }

        /// <summary>
        /// The items shipped in this waybill.
        /// </summary>
        public string? ShippedItems { get; set; }

        /// <summary>
        /// When this waybill record was created in the system.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this waybill record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether any investigations have detected anomalies in this waybill.
        /// </summary>
        public bool HasAnomalies { get; set; }

        /// <summary>
        /// When this waybill was last investigated, if ever.
        /// </summary>
        public DateTime? LastInvestigatedAt { get; set; }

        /// <summary>
        /// The due date for delivery of this waybill.
        /// Used by investigation algorithms to detect overdue and expiring deliveries.
        /// </summary>
        public DateTime? DueDate { get; set; }
    }
}