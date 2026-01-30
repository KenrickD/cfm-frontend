using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.DTOs.WorkCategory
{
    /// <summary>
    /// Response model for paginated Work Category list.
    /// Uses TypeFormDetailResponse which contains IdType, ParentTypeIdType, DisplayOrder, TypeName.
    /// </summary>
    public class WorkCategoryListResponse
    {
        public List<TypeFormDetailResponse>? Data { get; set; }
        public PagingInfo Metadata { get; set; }
    }
}
