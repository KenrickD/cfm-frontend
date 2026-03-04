namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Response DTO for job code details
    /// Maps to backend JobCodeDTO
    /// Returned by GET /api/v1/jobcodes/{id}
    /// </summary>
    public class JobCodeDTO
    {
        public int IdJobCode { get; set; }
        public int IdClient { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public JobCodeGroupDto Group { get; set; } = new JobCodeGroupDto();
        public JobCodeLookupDto Label { get; set; } = new JobCodeLookupDto();
        public JobCodeLookupDto? MaterialType { get; set; }
        public JobCodeEstimationTimeDto EstimationTime { get; set; } = new JobCodeEstimationTimeDto();
        public JobCodeUnitPriceDto UnitPrice { get; set; } = new JobCodeUnitPriceDto();
        public JobCodeLookupDto MeasurementUnit { get; set; } = new JobCodeLookupDto();
        public double? MinimumStock { get; set; }
    }

    /// <summary>
    /// Job code lookup DTO for dropdowns and references
    /// </summary>
    public class JobCodeLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Job code unit price DTO
    /// </summary>
    public class JobCodeUnitPriceDto
    {
        public int Currency_IdEnum { get; set; }
        public string Currency { get; set; } = string.Empty;
        public double Price { get; set; }
    }
}
