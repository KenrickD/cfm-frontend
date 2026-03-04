using cfm_frontend.DTOs.JobCode;
using cfm_frontend.Models;
using cfm_frontend.Models.JobCode;

namespace cfm_frontend.ViewModels
{
    public class JobCodeAddViewModel
    {
        public List<JobCodeGroupDto>? Groups { get; set; }
        public List<LookupModel>? Currencies { get; set; }
        public List<LookupModel>? MeasurementUnits { get; set; }
        public List<LookupModel>? MaterialLabels { get; set; }
    }
}
