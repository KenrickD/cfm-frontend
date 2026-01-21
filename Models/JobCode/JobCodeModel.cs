namespace cfm_frontend.Models.JobCode
{
    public class JobCodeModel
    {
        public int IdJobCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Group { get; set; }
        public string? LaborOrMaterial { get; set; }
        public string? MaterialType { get; set; }
        public string? EstimationTime { get; set; }
        public string? MeasurementUnit { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public decimal? MinimumStock { get; set; }
        public bool IsActiveData { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Model for Job Code search results used in Labor/Material modal
    /// Maps to backend JobCodeFormDetailResponse
    /// </summary>
    public class JobCodeSearchResult
    {
        public int IdJobCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public float UnitPrice { get; set; }
        public string UnitPriceCurrency { get; set; }
        public double MinimumStock { get; set; }
        public double LatestStock { get; set; }
        public string? LaborMaterialMeasurementUnit { get; set; }
    }
}
