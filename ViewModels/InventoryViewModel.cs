using cfm_frontend.DTOs.Inventory;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// View model for Inventory Management Index page
    /// Aggregates transactions list, paging info, and client context
    /// </summary>
    public class InventoryViewModel
    {
        public List<InventoryListDto>? Transactions { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
