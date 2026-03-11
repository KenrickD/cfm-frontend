namespace cfm_frontend.DTOs.WorkCategoryRelation
{
    /// <summary>
    /// List item DTO for Work Category Relation
    /// Used in paginated list view
    /// </summary>
    public class WorkCategoryRelationListDto
    {
        public int IdWorkCategoryRelation { get; set; }
        public string WorkCategoryName { get; set; } = string.Empty;
        public string PriorityLevelName { get; set; } = string.Empty;
        public string PICName { get; set; } = string.Empty;
    }
}
