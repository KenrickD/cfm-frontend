namespace cfm_frontend.Models.WorkRequest
{
    /// <summary>
    /// Response model for Enums API endpoint
    /// Uses PascalCase naming with PropertyNameCaseInsensitive deserialization
    /// </summary>
    public class EnumFormDetailResponse
    {
        public int IdEnum { get; set; }
        public int? ParentEnumIdEnum { get; set; }
        public int? DisplayOrder { get; set; }
        public string EnumName { get; set; }
    }
}
