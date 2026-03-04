namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Request DTO for updating an existing job code
    /// Maps to backend JobCodePayloadDto structure
    /// </summary>
    public class JobCodeUpdateRequest
    {
        public int? IdJobCode { get; set; }
        public int IdClient { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Group_IdType { get; set; }
        public int Label_IdEnum { get; set; }
        public int? MaterialType_IdType { get; set; }
        public int MeasurementUnit_IdEnum { get; set; }
        public int Currency_IdEnum { get; set; }
        public double UnitPrice { get; set; }
        public double? MinimumStock { get; set; }
        public JobCodeEstimationTimeDto EstimationTime { get; set; } = new JobCodeEstimationTimeDto();
    }
}
