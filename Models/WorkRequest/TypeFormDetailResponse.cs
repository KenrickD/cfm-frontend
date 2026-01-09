namespace cfm_frontend.Models.WorkRequest
{
    /// <summary>
    /// Response model for Types API endpoint
    /// Uses PascalCase naming with PropertyNameCaseInsensitive deserialization
    /// </summary>
    public class TypeFormDetailResponse
    {
        public int IdType { get; set; }
        public int? ParentTypeIdType { get; set; }
        public int? DisplayOrder { get; set; }
        public string TypeName { get; set; }
    }
}
