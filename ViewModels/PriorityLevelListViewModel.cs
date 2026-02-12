using cfm_frontend.DTOs.PriorityLevel;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Priority Level settings list page with pagination support.
    /// </summary>
    public class PriorityLevelListViewModel
    {
        public List<PriorityLevelDetailsDto>? PriorityLevels { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety.
        /// </summary>
        public int IdClient { get; set; }
    }
}
