namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Response DTO for job code list filters
    /// Maps to backend JobCodeListFilterDto
    /// Returned by GET /api/v1/jobcodes/list-filter
    /// </summary>
    public class JobCodeListFilterDto
    {
        public List<JobCodeGroupDto> Groups { get; set; } = new List<JobCodeGroupDto>();
    }

    /// <summary>
    /// Job code group DTO for filter dropdowns
    /// Maps to backend JobCodeGroupDto
    /// </summary>
    public class JobCodeGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
