using cfm_frontend.Models.JobCode;

namespace cfm_frontend.DTOs.JobCode
{
    public class JobCodeListResponse
    {
        public List<JobCodeModel>? Data { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
