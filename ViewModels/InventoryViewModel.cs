using cfm_frontend.Models;
using cfm_frontend.Models.Inventory;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// View model for Inventory Management Index page
    /// Aggregates transactions list, paging info, and filter options
    /// </summary>
    public class InventoryViewModel
    {
        public List<InventoryTransactionResponseDto>? Transactions { get; set; }
        public PagingInfo? Paging { get; set; }
        public InventoryFilterOptionsModel? FilterOptions { get; set; }
    }
}
