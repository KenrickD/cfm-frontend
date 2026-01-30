using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Work Category settings page with pagination support.
    /// </summary>
    public class WorkCategoryViewModel
    {
        public List<TypeFormDetailResponse>? Categories { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety.
        /// </summary>
        public int IdClient { get; set; }
    }
}
