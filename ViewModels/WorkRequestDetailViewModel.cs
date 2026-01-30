using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestDetailViewModel
    {
        /// <summary>
        /// The main work request data from API response
        /// </summary>
        public WorkRequestFormDetailDto? WorkRequestDetail { get; set; }

        /// <summary>
        /// Change history list (for future use)
        /// </summary>
        public List<ChangeHistory>? ChangeHistories { get; set; }
    }
}
