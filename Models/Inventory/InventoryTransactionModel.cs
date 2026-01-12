namespace cfm_frontend.Models.Inventory
{
    /// <summary>
    /// Request model for creating inventory transaction
    /// </summary>
    public class InventoryTransactionCreateRequest
    {
        public int Client_idClient { get; set; }
        public string stockMovementType { get; set; } = string.Empty;
        public DateTime transactionDate { get; set; }
        public int idMaterial { get; set; }
        public decimal quantity { get; set; }
        public string description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for transaction create/update
    /// </summary>
    public class InventoryTransactionSaveResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public int? idInventoryTransaction { get; set; }
    }

    /// <summary>
    /// Material search result for autocomplete
    /// </summary>
    public class MaterialSearchResultDto
    {
        public int idMaterial { get; set; }
        public string materialCode { get; set; } = string.Empty;
        public string materialName { get; set; } = string.Empty;
        public decimal remainingStock { get; set; }
        public string unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Material search response wrapper
    /// </summary>
    public class MaterialSearchResponse
    {
        public bool success { get; set; }
        public List<MaterialSearchResultDto> data { get; set; } = new List<MaterialSearchResultDto>();
        public string message { get; set; } = string.Empty;
    }
}
