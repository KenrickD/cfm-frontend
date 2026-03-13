namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Maintenance Schedule Detail page
    /// </summary>
    public class MaintenanceScheduleDetailViewModel
    {
        /// <summary>
        /// Client ID for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Schedule identifier
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// Schedule code (e.g., PM-2112034)
        /// </summary>
        public string ScheduleCode { get; set; } = string.Empty;

        /// <summary>
        /// Activity name
        /// </summary>
        public string ActivityName { get; set; } = string.Empty;

        /// <summary>
        /// Location/Building name
        /// </summary>
        public string Location { get; set; } = string.Empty;

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
        /// Schedule status
        /// </summary>
        public string Status { get; set; } = "New";

        /// <summary>
        /// Service provider name
        /// </summary>
        public string ServiceProvider { get; set; } = string.Empty;

        /// <summary>
        /// Frequency description
        /// </summary>
        public string Frequency { get; set; } = string.Empty;

        /// <summary>
        /// Notes/Comments
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Attachments/Documents
        /// </summary>
        public List<ScheduleAttachmentDto>? Attachments { get; set; }

        /// <summary>
        /// Activity ID (for navigation/editing)
        /// </summary>
        public int ActivityId { get; set; }
    }

    /// <summary>
    /// Schedule Attachment DTO
    /// </summary>
    public class ScheduleAttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
    }
}
