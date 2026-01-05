using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestViewModel
    {
        public List<WorkRequestResponseModel>? WorkRequest { get; set; }
        public PagingInfo? Paging { get; set; }
        public FilterOptionsModel? FilterOptions { get; set; }
        public List<LocationModel>? Locations { get; set; }
        public List<ServiceProviderModel>? ServiceProviders { get; set; }
        public List<WorkCategoryModel>? WorkCategories { get; set; }
        public List<OtherCategoryModel>? OtherCategories { get; set; }
    }
}
