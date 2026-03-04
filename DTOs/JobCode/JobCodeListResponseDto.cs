namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Response DTO for individual job code in list view
    /// Maps to backend JobCodeListResponseDto
    /// Returned by POST /api/v1/jobcodes/list
    /// </summary>
    public class JobCodeListResponseDto
    {
        public int IdGroup { get; set; }
        public string Group { get; set; } = string.Empty;
        public int IdJobCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public string EstimationTime { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;
        public string UnitPrice { get; set; } = string.Empty;
    }
}
