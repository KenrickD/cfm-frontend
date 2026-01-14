using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestViewModel
    {
        // Work Request List
        public List<WorkRequestResponseModel>? WorkRequest { get; set; }
        public PagingInfo? Paging { get; set; }
        public FilterOptionsModel? FilterOptions { get; set; }

        // Location and Service Provider
        public List<LocationModel>? Locations { get; set; }
        public List<ServiceProviderModel>? ServiceProviders { get; set; }

        // Work Categories
        public List<TypeFormDetailResponse>? WorkCategories { get; set; }
        public List<TypeFormDetailResponse>? OtherCategories { get; set; }
        public List<TypeFormDetailResponse>? OtherCategories2 { get; set; }

        // Priority Level with full details for target date calculations
        public List<Models.PriorityLevelModel>? PriorityLevels { get; set; }

        // Enums (Request Methods, Statuses, Feedback Types)
        public List<EnumFormDetailResponse>? FeedbackTypes { get; set; }
        public List<EnumFormDetailResponse>? RequestMethods { get; set; }
        public List<EnumFormDetailResponse>? Statuses { get; set; }

        // Lookup data (Currencies)
        public List<LookupModel>? Currencies { get; set; }

        // Important Checklist
        public List<TypeFormDetailResponse>? ImportantChecklist { get; set; }
    }
}
