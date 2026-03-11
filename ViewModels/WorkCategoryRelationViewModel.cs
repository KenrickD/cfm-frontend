using cfm_frontend.DTOs.WorkCategoryRelation;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Work Category Relation list page with pagination support
    /// </summary>
    public class WorkCategoryRelationViewModel
    {
        public List<WorkCategoryRelationListDto>? Relations { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
