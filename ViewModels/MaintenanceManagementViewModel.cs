using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Maintenance Management calendar page
    /// </summary>
    public class MaintenanceManagementViewModel
    {
        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Property groups for filter dropdown
        /// </summary>
        public List<PropertyGroupDto> PropertyGroups { get; set; } = new List<PropertyGroupDto>();

        /// <summary>
        /// Buildings for filter dropdown
        /// </summary>
        public List<BuildingDto> Buildings { get; set; } = new List<BuildingDto>();

        /// <summary>
        /// Calendar view mode (52-Week View, Monthly View, etc.)
        /// </summary>
        public string ViewMode { get; set; } = "52-Week View";

        /// <summary>
        /// Start date for calendar range
        /// </summary>
        public DateTime FromDate { get; set; }

        /// <summary>
        /// List of maintenance activities (optional, can be loaded via AJAX)
        /// </summary>
        public List<MaintenanceActivityDto>? Activities { get; set; }

        /// <summary>
        /// Pagination information (if activities are preloaded)
        /// </summary>
        public PagingInfo? Paging { get; set; }
    }

    /// <summary>
    /// Property Group DTO for filter
    /// </summary>
    public class PropertyGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Building DTO for filter
    /// </summary>
    public class BuildingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? PropertyGroupId { get; set; }
    }
}
