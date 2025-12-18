using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestViewModel
    {
        public List<PropertyGroupModel>? PropertyGroups { get; set; }
        public List<WRStatusModel>? Status { get; set; }
        public List<WorkRequestResponseModel>? WorkRequest { get; set; }
    }
}
