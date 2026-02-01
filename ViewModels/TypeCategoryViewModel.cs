using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// Generic ViewModel for Type-based category settings pages.
    /// Used by OtherCategory and OtherCategory2 pages.
    /// </summary>
    public class TypeCategoryViewModel
    {
        public List<TypeFormDetailResponse>? Categories { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety.
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Category type identifier for display purposes (e.g., "Other Category", "Other Category 2")
        /// </summary>
        public string CategoryDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Plural form for display (e.g., "Other Categories", "Other Categories 2")
        /// </summary>
        public string CategoryDisplayNamePlural { get; set; } = string.Empty;
    }
}
