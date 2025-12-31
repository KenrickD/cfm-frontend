using cfm_frontend.Models;
using cfm_frontend.Models.JobCode;

namespace cfm_frontend.ViewModels
{
    public class JobCodeDetailViewModel
    {
        public JobCodeModel? JobCode { get; set; }
        public List<ChangeHistoryModel>? ChangeHistory { get; set; }
    }
}
