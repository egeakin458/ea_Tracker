using ea_Tracker.Enums;

namespace ea_Tracker.Models
{
    /// <summary>
    /// Represents a waybill for shipped goods.
    /// </summary>
    public class Waybill
    {
        public int Id { get; set; }
        public string? RecipientName { get; set; }
        public DateTime GoodsIssueDate { get; set; }
        public WaybillType WaybillType { get; set; }
        public string? ShippedItems { get; set; }
    }
}
