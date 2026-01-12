namespace cfm_frontend.Models.Inventory
{
    /// <summary>
    /// Filter options for inventory transactions
    /// </summary>
    public class InventoryFilterOptionsModel
    {
        public List<TransactionStatusModel> TransactionStatuses { get; set; } = new List<TransactionStatusModel>();
        public List<WorkRequestFilterModel> WorkRequests { get; set; } = new List<WorkRequestFilterModel>();
    }

    /// <summary>
    /// Transaction status option for filter
    /// </summary>
    public class TransactionStatusModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    /// <summary>
    /// Work request option for filter
    /// </summary>
    public class WorkRequestFilterModel
    {
        public int idWorkRequest { get; set; }
        public string workRequestCode { get; set; } = string.Empty;
        public string workTitle { get; set; } = string.Empty;
    }
}
