namespace cfm_frontend.DTOs.WorkCategoryRelation
{
    /// <summary>
    /// Detailed DTO for Work Category Relation
    /// Includes all target sections and assigned PICs
    /// </summary>
    public class WorkCategoryRelationDetailDto
    {
        public int IdWorkCategoryRelation { get; set; }
        public string WorkCategoryName { get; set; } = string.Empty;
        public string PriorityLevelName { get; set; } = string.Empty;
        public string AssignedPIC { get; set; } = string.Empty;
        public List<string> Locations { get; set; } = new List<string>();

        public List<PICTargetDto> HelpdeskResponseTargets { get; set; } = new List<PICTargetDto>();
        public List<PICTargetDto> InitialFollowUpTargets { get; set; } = new List<PICTargetDto>();
        public List<PICTargetDto> QuotationSubmissionTargets { get; set; } = new List<PICTargetDto>();
        public List<PICTargetDto> CostApprovalTargets { get; set; } = new List<PICTargetDto>();
        public List<PICTargetDto> WorkCompletionTargets { get; set; } = new List<PICTargetDto>();
        public List<PICTargetDto> AfterWorkFollowUpTargets { get; set; } = new List<PICTargetDto>();
    }

    /// <summary>
    /// PIC assigned to a specific target section
    /// </summary>
    public class PICTargetDto
    {
        public int IdPIC { get; set; }
        public string PICName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
