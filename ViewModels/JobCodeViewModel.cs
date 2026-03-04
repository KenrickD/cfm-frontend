using cfm_frontend.DTOs.JobCode;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    public class JobCodeViewModel
    {
        public List<JobCodeListResponseDto>? JobCodes { get; set; }
        public List<JobCodeGroupDto>? Groups { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }
        public string? SelectedGroup { get; set; }
        public bool? ShowDeletedData { get; set; }
    }
}
