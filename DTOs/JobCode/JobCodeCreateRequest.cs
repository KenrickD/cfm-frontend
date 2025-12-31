namespace cfm_frontend.DTOs.JobCode
{
    public class JobCodeCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Group { get; set; } = string.Empty;
        public string LaborOrMaterial { get; set; } = string.Empty;
        public string? MaterialType { get; set; }
        public int? EstimationTimeDays { get; set; }
        public int? EstimationTimeHours { get; set; }
        public int? EstimationTimeMinutes { get; set; }
        public string MeasurementUnit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? MinimumStock { get; set; }
        public int IdClient { get; set; }
    }
}
