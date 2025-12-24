using cfm_frontend.Models.WorkRequest;
using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestDetailViewModel
    {
        // The main work request data (could also reuse WorkRequestCreateRequest as the structure)
        public WorkRequestResponseModel WorkRequest { get; set; }

        // Change history list
        public List<ChangeHistory>? ChangeHistories { get; set; }

        // Additional dropdown data for the form
        public List<LocationModel>? Locations { get; set; }
        public List<ServiceProviderModel>? ServiceProviders { get; set; }
        public List<WorkCategoryModel>? WorkCategories { get; set; }
        public List<OtherCategoryModel>? OtherCategories { get; set; }
    }
}
