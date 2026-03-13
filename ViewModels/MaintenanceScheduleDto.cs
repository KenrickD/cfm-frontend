namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// Maintenance Schedule DTO for calendar tile rendering
    /// </summary>
    public class MaintenanceScheduleDto
    {
        /// <summary>
        /// Unique schedule identifier
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// Related activity ID
        /// </summary>
        public int ActivityId { get; set; }

        /// <summary>
        /// Activity name (for tooltip)
        /// </summary>
        public string ActivityName { get; set; } = string.Empty;

        /// <summary>
        /// Planned start date
        /// </summary>
        public DateTime PlannedStartDate { get; set; }

        /// <summary>
        /// Planned end date
        /// </summary>
        public DateTime PlannedEndDate { get; set; }

        /// <summary>
        /// Actual start date (null if not started)
        /// </summary>
        public DateTime? ActualStartDate { get; set; }

        /// <summary>
        /// Actual end date (null if not completed)
        /// </summary>
        public DateTime? ActualEndDate { get; set; }

        /// <summary>
        /// Schedule status (New, In Progress, Completed, Overdue)
        /// </summary>
        public string Status { get; set; } = "New";

        /// <summary>
        /// Duration in days (for tile width calculation)
        /// </summary>
        public int Duration { get; set; } = 1;

        /// <summary>
        /// Notes/Comments
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Schedule Tooltip DTO for hover preview
    /// </summary>
    public class ScheduleTooltipDto
    {
        /// <summary>
        /// Schedule code (e.g., PM-2112034)
        /// </summary>
        public string ScheduleCode { get; set; } = string.Empty;

        /// <summary>
        /// Planned date range formatted string
        /// </summary>
        public string PlannedDate { get; set; } = string.Empty;

        /// <summary>
        /// Actual date formatted string (or "-" if not started)
        /// </summary>
        public string ActualDate { get; set; } = "-";

        /// <summary>
        /// Status (New, In Progress, Completed, Overdue)
        /// </summary>
        public string Status { get; set; } = "New";
    }
}
