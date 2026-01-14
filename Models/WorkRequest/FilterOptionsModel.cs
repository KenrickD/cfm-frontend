using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace cfm_frontend.Models.WorkRequest
{
    public class FilterOptionsModel
    {
        public List<PropertyGroupModel> PropertyGroups { get; set; } = new List<PropertyGroupModel>();
        public List<LocationModel> Locations { get; set; } = new List<LocationModel>();
        public List<ServiceProviderModel> ServiceProviders { get; set; } = new List<ServiceProviderModel>();
        public List<RoomZoneModel> RoomZones { get; set; } = new List<RoomZoneModel>();
        public List<WorkCategoryModel> WorkCategories { get; set; } = new List<WorkCategoryModel>();
        public List<OtherCategoryModel> OtherCategories { get; set; } = new List<OtherCategoryModel>();
        public List<FilterPriorityModel> PriorityLevels { get; set; } = new List<FilterPriorityModel>();
        public List<FilterStatusModel> Statuses { get; set; } = new List<FilterStatusModel>();
        public List<FilterChecklistModel> ImportantChecklists { get; set; } = new List<FilterChecklistModel>();
        public List<FilterFeedbackModel> FeedbackTypes { get; set; } = new List<FilterFeedbackModel>();
        public List<FilterRequestMethodModel> RequestMethods { get; set; } = new List<FilterRequestMethodModel>();
    }

    public class FilterPriorityModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class FilterStatusModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class FilterChecklistModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class FilterFeedbackModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class RoomZoneModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class FilterRequestMethodModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
