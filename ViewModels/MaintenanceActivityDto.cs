namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// Maintenance Activity DTO for calendar list view
    /// </summary>
    public class MaintenanceActivityDto
    {
        /// <summary>
        /// Unique activity identifier
        /// </summary>
        public int ActivityId { get; set; }

        /// <summary>
        /// Activity name/description
        /// </summary>
        public string ActivityName { get; set; } = string.Empty;

        /// <summary>
        /// Location/Building name where activity is performed
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Frequency description (e.g., "1 Week", "2 Weeks", "1 Month", "3 Months")
        /// </summary>
        public string Frequency { get; set; } = string.Empty;

        /// <summary>
        /// Service provider name (e.g., "Self-Performed", "External Contractor")
        /// </summary>
        public string ServiceProvider { get; set; } = string.Empty;

        /// <summary>
        /// Property Group ID (for filtering)
        /// </summary>
        public int? PropertyGroupId { get; set; }

        /// <summary>
        /// Building ID (for filtering)
        /// </summary>
        public int? BuildingId { get; set; }

        /// <summary>
        /// Whether activity is active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
