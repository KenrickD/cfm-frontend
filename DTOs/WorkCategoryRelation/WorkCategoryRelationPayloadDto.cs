namespace cfm_frontend.DTOs.WorkCategoryRelation
{
    /// <summary>
    /// Payload DTO for creating or updating Work Category Relation
    /// </summary>
    public class WorkCategoryRelationPayloadDto
    {
        public int IdWorkCategoryRelation { get; set; }
        public int IdWorkCategory { get; set; }
        public int IdPriorityLevel { get; set; }
        public int IdPIC { get; set; }
        public int IdClient { get; set; }
        public List<int> PropertyIds { get; set; } = new List<int>();
    }
}
