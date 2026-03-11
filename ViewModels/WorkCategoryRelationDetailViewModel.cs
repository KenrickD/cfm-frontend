using cfm_frontend.DTOs.WorkCategoryRelation;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Work Category Relation Detail page
    /// Displays complete relation info with all target sections
    /// </summary>
    public class WorkCategoryRelationDetailViewModel
    {
        public int IdWorkCategoryRelation { get; set; }
        public string WorkCategoryName { get; set; } = string.Empty;
        public string PriorityLevelName { get; set; } = string.Empty;
        public string AssignedPIC { get; set; } = string.Empty;
        public List<string> Locations { get; set; } = new List<string>();

        /// <summary>
        /// PICs for Helpdesk Response Target section
        /// </summary>
        public List<PICTargetDto> HelpdeskResponseTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// PICs for Initial Follow Up Target section
        /// </summary>
        public List<PICTargetDto> InitialFollowUpTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// PICs for Quotation Submission Target section
        /// </summary>
        public List<PICTargetDto> QuotationSubmissionTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// PICs for Cost Approval Target section
        /// </summary>
        public List<PICTargetDto> CostApprovalTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// PICs for Work Completion Target section
        /// </summary>
        public List<PICTargetDto> WorkCompletionTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// PICs for After Work Follow Up Target section
        /// </summary>
        public List<PICTargetDto> AfterWorkFollowUpTargets { get; set; } = new List<PICTargetDto>();

        /// <summary>
        /// Client ID for session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
