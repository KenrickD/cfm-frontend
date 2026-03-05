using cfm_frontend.DTOs.JobCode;

namespace cfm_frontend.DTOs.Inventory
{
    /// <summary>
    /// Response DTO for inventory transaction details
    /// Maps to backend InventoryDetailsDto in InventoryModelDTO.cs
    /// Returned by GET /api/v1/inventories/{id}
    /// </summary>
    public class InventoryDetailsDto
    {
        public int IdInventoryTransactionHistory { get; set; }
        public int IdClient { get; set; }
        public int InventoryStatus_IdEnum { get; set; }
        public DateTime TransactionDate { get; set; }
        public JobCodeLookupDto Material { get; set; } = new JobCodeLookupDto();
        public double Quantity { get; set; }
        public string? Description { get; set; }
    }
}
