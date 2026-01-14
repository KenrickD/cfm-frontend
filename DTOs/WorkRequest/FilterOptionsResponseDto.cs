namespace cfm_frontend.DTOs.WorkRequest
{
    /// <summary>
    /// Main data object containing all filter options
    /// Wrapped in BaseSuccessResponse from backend API
    /// </summary>
    public class FilterOptionsDataDto
    {
        public List<FeedbackTypeDto> FeedbackTypes { get; set; } = new();
        public List<ImportantChecklistDto> ImportantChecklists { get; set; } = new();
        public List<LocationGroupDto> Locations { get; set; } = new();
        public List<OtherCategoryDto> OtherCategories { get; set; } = new();
        public List<PriorityLevelDto> PriorityLevels { get; set; } = new();
        public List<ServiceProviderDto> ServiceProviders { get; set; } = new();
        public List<StatusDto> Statuses { get; set; } = new();
        public List<WorkCategoryDto> WorkCategories { get; set; } = new();
        public List<RequestMethodDto> RequestMethods { get; set; } = new();

        // Response flags from backend
        public bool HasWorkUpdateEmailSent { get; set; }
        public bool IncludeDeletedData { get; set; }
        public bool IncludeWorkCompletionDate { get; set; }
        public bool IncludeRequestDate { get; set; }
        public bool IncludeCompletedByWorkers { get; set; }
    }

    #region Nested Location Structure

    /// <summary>
    /// Property group containing multiple properties
    /// </summary>
    public class LocationGroupDto
    {
        public int PropertyGroupId { get; set; }
        public string PropertyGroup { get; set; } = string.Empty;
        public List<PropertyDto> Properties { get; set; } = new();
    }

    /// <summary>
    /// Property (location) containing multiple room zones
    /// </summary>
    public class PropertyDto
    {
        public int PropertyId { get; set; }
        public string Property { get; set; } = string.Empty;
        public List<RoomZoneDto> RoomZones { get; set; } = new();
    }

    /// <summary>
    /// Room zone with floor information
    /// </summary>
    public class RoomZoneDto
    {
        public int RoomZoneId { get; set; }
        public int PropertyFloorId { get; set; }
        public int BuildingFloorId { get; set; }
        public string FloorUnit { get; set; } = string.Empty;
        public string RoomZone { get; set; } = string.Empty;
    }

    #endregion

    #region Simple Filter DTOs

    /// <summary>
    /// Feedback type filter option
    /// </summary>
    public class FeedbackTypeDto
    {
        public int IdFeedbackType { get; set; }
        public string FeedbackType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Important checklist filter option
    /// </summary>
    public class ImportantChecklistDto
    {
        public int IdImportantChecklist { get; set; }
        public string ImportantChecklist { get; set; } = string.Empty;
    }

    /// <summary>
    /// Other category filter option
    /// </summary>
    public class OtherCategoryDto
    {
        public int IdOtherCategory { get; set; }
        public string OtherCategory { get; set; } = string.Empty;
    }

    /// <summary>
    /// Priority level filter option
    /// </summary>
    public class PriorityLevelDto
    {
        public int IdPriorityLevel { get; set; }
        public string PriorityLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service provider filter option
    /// </summary>
    public class ServiceProviderDto
    {
        public int IdServiceProvider { get; set; }
        public string ServiceProvider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Work request status filter option
    /// </summary>
    public class StatusDto
    {
        public int IdWorkRequestStatus { get; set; }
        public string WorkRequestStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Work category filter option
    /// </summary>
    public class WorkCategoryDto
    {
        public int IdWorkCategory { get; set; }
        public string WorkCategory { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request method filter option (e.g., Phone, Email, Web)
    /// </summary>
    public class RequestMethodDto
    {
        public int IdRequestMethod { get; set; }
        public string RequestMethod { get; set; } = string.Empty;
    }

    #endregion
}
