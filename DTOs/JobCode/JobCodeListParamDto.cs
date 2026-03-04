namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Request DTO for getting paginated job code list
    /// Maps to backend JobCodeListParamDto
    /// Used with POST /api/v1/jobcodes/list (backend has bug: uses [HttpGet] with [FromBody])
    /// </summary>
    public class JobCodeListParamDto
    {
        public int IdClient { get; set; }
        public string? Keywords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public JobCodeListFilterParamDto? Filter { get; set; }

        public JobCodeListParamDto()
        {
            Page = 1;
            PageSize = 25;
            Filter = new JobCodeListFilterParamDto();
        }
    }

    /// <summary>
    /// Filter parameters for job code list
    /// </summary>
    public class JobCodeListFilterParamDto
    {
        public int[]? Groups { get; set; }
    }
}
