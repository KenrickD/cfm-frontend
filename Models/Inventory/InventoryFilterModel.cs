using cfm_frontend.Models;

namespace cfm_frontend.Models.Inventory
{
    /// <summary>
    /// Request body model for inventory transaction filtering
    /// </summary>
    public class InventoryFilterModel
    {
        public int Client_idClient { get; set; }
        public int page { get; set; } = 1;
        public string keyWordSearch { get; set; } = string.Empty;

        public List<string> TransactionStatuses { get; set; } = new List<string>();
        public List<int> WorkRequestIds { get; set; } = new List<int>();
        public DateTime? transactionDateFrom { get; set; }
        public DateTime? transactionDateTo { get; set; }
    }

    /// <summary>
    /// Response model for individual inventory transaction
    /// </summary>
    public class InventoryTransactionResponseDto
    {
        public int idInventoryTransaction { get; set; }
        public DateTime transactionDate { get; set; }
        public string transactionStatus { get; set; } = string.Empty;
        public string materialName { get; set; } = string.Empty;
        public decimal quantity { get; set; }
        public string description { get; set; } = string.Empty;

        public int? idWorkRequest { get; set; }
        public string? workRequestCode { get; set; }
    }

    /// <summary>
    /// API response wrapper for paginated inventory list
    /// </summary>
    public class InventoryListApiResponse
    {
        public List<InventoryTransactionResponseDto> data { get; set; } = new List<InventoryTransactionResponseDto>();
        public PagingInfo Metadata { get; set; } = new PagingInfo();
    }
}
