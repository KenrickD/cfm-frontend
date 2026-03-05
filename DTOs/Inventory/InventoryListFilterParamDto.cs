namespace cfm_frontend.DTOs.Inventory
{
    /// <summary>
    /// Filter parameters for inventory transaction list
    /// Maps to backend InventoryListFilterParamDto in InventoryModelDTO.cs
    /// Used within InventoryListParamDto
    /// </summary>
    public class InventoryListFilterParamDto
    {
        public int[]? MovementTypes { get; set; }
        public DateTime? TransactionDateStart { get; set; }
        public DateTime? TransactionDateEnd { get; set; }
    }
}
