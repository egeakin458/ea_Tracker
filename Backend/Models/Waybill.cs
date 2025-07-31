using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a waybill for shipped goods.
    /// </summary>
    public class Waybill
    {
        /// <summary>
        /// Gets or sets the unique identifier for the waybill.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the recipient.
        /// </summary>
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
        public string? ShippedItems { get; set; }
    }
}
