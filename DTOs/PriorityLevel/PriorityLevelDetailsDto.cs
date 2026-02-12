using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.DTOs.PriorityLevel
{
    /// <summary>
    /// Nested DTO matching backend PriorityLevelDetailsDto structure.
    /// Used for CRUD operations with the new /api/v1/work-request/priority-levels endpoints.
    /// </summary>
    public class PriorityLevelDetailsDto
    {
        public int IdPriorityLevel { get; set; }
        public int IdClient { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DisplayOrder { get; set; }
        public PriorityLevelTargetBaseDto? HelpdeskResponseTarget { get; set; }
        public PriorityLevelTargetBaseDto? QuotationSubmissionTarget { get; set; }
        public PriorityLevelTargetBaseDto? WorkCompletionTarget { get; set; }
        public PriorityLevelTargetBaseDto? InitialFollowUpTarget { get; set; }
        public PriorityLevelTargetBaseDto? CostApprovalTarget { get; set; }
        public PriorityLevelTargetBaseDto? AfterWorkFollowUpTarget { get; set; }
        public TypeFormDetailResponse? VisualColor { get; set; }

        public PriorityLevelDetailsDto()
        {
            HelpdeskResponseTarget = new PriorityLevelTargetBaseDto();
            QuotationSubmissionTarget = new PriorityLevelTargetBaseDto();
            WorkCompletionTarget = new PriorityLevelTargetBaseDto();
            InitialFollowUpTarget = new PriorityLevelTargetBaseDto();
            CostApprovalTarget = new PriorityLevelTargetBaseDto();
            AfterWorkFollowUpTarget = new PriorityLevelTargetBaseDto();
        }
    }

    /// <summary>
    /// Target configuration for a priority level section (e.g., Helpdesk Response, Work Completion).
    /// </summary>
    public class PriorityLevelTargetBaseDto
    {
        public PriorityLevelTargetDurationDto? Duration { get; set; }
        public bool? WithinOfficeHours { get; set; }
        public EnumFormDetailResponse? BaseTarget { get; set; }
        public PriorityLevelTargetDurationDto? ComplianceTarget { get; set; }
        public bool? RequiredToFillCompletion { get; set; }
        public bool? AcknowledgeWhenActualFilled { get; set; }
        public bool? AcknowledgeWhenTargetChanged { get; set; }
        public bool? IsAutoFill { get; set; }
        public PriorityLevelTargetDurationDto? ReminderBeforeTarget { get; set; }

        public PriorityLevelTargetBaseDto()
        {
            Duration = null;
            ComplianceTarget = null;
            ReminderBeforeTarget = null;
            BaseTarget = new EnumFormDetailResponse();
            WithinOfficeHours = false;
            RequiredToFillCompletion = false;
            AcknowledgeWhenActualFilled = false;
            AcknowledgeWhenTargetChanged = false;
            IsAutoFill = false;
        }
    }

    /// <summary>
    /// Duration specification in days, hours, minutes (and optional ticks for backend compatibility).
    /// </summary>
    public class PriorityLevelTargetDurationDto
    {
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Ticks { get; set; }

        public PriorityLevelTargetDurationDto()
        {
            Days = 0;
            Hours = 0;
            Minutes = 0;
            Ticks = 0;
        }
    }

    /// <summary>
    /// Paginated response wrapper for priority level list.
    /// </summary>
    public class PriorityLevelPagedResponse
    {
        public List<PriorityLevelDetailsDto>? Data { get; set; }
        public PriorityLevelPagingMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Pagination metadata for priority level list responses.
    /// </summary>
    public class PriorityLevelPagingMetadata
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
