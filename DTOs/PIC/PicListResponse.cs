using cfm_frontend.Models;

namespace cfm_frontend.DTOs.PIC
{
    /// <summary>
    /// Paginated response wrapper for PIC list.
    /// Follows the same pattern as WorkCategoryListResponse.
    /// </summary>
    public class PicListResponse
    {
        public List<PicPropertySummaryDto>? Data { get; set; }
        public PagingInfo Metadata { get; set; } = new();
    }
}
