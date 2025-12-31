using cfm_frontend.Models;
using cfm_frontend.Models.JobCode;

namespace cfm_frontend.ViewModels
{
    public class JobCodeViewModel
    {
        public List<JobCodeModel>? JobCodes { get; set; }
        public List<JobCodeGroupModel>? Groups { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }
        public string? SelectedGroup { get; set; }
        public bool? ShowDeletedData { get; set; }
    }
}
