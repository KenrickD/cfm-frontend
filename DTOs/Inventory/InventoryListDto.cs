namespace cfm_frontend.DTOs.Inventory
{
    /// <summary>
    /// Response DTO for individual inventory transaction in list view
    /// Maps to backend InventoryListDto in InventoryModelDTO.cs
    /// Returned by GET /api/v1/inventories/list
    /// </summary>
    public class InventoryListDto
    {
        public int IdInventoryTransactionHistory { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? WorkRequetCode { get; set; }
        public int? IdWorkRequest { get; set; }
        public string Material { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string MeasurementUnit { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
