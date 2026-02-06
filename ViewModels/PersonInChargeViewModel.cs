using cfm_frontend.DTOs.PIC;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Person in Charge settings page with pagination support.
    /// </summary>
    public class PersonInChargeViewModel
    {
        public List<PicPropertySummaryDto>? PersonsInCharge { get; set; }
        public PagingInfo? Paging { get; set; }
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety.
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Company ID captured at page load for employee search.
        /// </summary>
        public int IdCompany { get; set; }
    }
}
