using cfm_frontend.Models;
using static cfm_frontend.Models.WorkRequestFilterModel;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestViewModel
    {
        public List<PropertyGroupModel>? PropertyGroups { get; set; }
        public List<WRStatusModel>? Status { get; set; }
        public List<WorkRequestResponseModel>? WorkRequest { get; set; }
        public List<LocationModel>? Locations { get; set; }
        public List<ServiceProviderModel>? ServiceProviders { get; set; }
        public List<WorkCategoryModel>? WorkCategories { get; set; }
        public List<OtherCategoryModel>? OtherCategories { get; set; }
        public PagingInfo? Paging { get; set; }
    }
}
