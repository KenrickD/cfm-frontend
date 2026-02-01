using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.DTOs.TypeSettings
{
    /// <summary>
    /// Response model for paginated Type-based category lists.
    /// Shared by WorkCategory, OtherCategory, OtherCategory2.
    /// </summary>
    public class TypeCategoryListResponse
    {
        public List<TypeFormDetailResponse>? Data { get; set; }
        public PagingInfo Metadata { get; set; } = new();
    }
}
