namespace cfm_frontend.DTOs.Inventory
{
    /// <summary>
    /// Request DTO for inventory transaction list with filters and pagination
    /// Maps to backend InventoryListParamDto in InventoryModelDTO.cs
    /// Used by GET /api/v1/inventories/list
    /// </summary>
    public class InventoryListParamDto
    {
        public int IdClient { get; set; }
        public string? Keywords { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public InventoryListFilterParamDto? Filters { get; set; }
    }
}
