using cfm_frontend.DTOs.CompanyContact;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Company Contact Edit page
    /// </summary>
    public class CompanyContactEditViewModel
    {
        /// <summary>
        /// Contact data
        /// </summary>
        public CompanyContactDto? Contact { get; set; }

        /// <summary>
        /// Available title prefixes (Mr, Ms, Mrs, etc.)
        /// </summary>
        public List<SelectListItem>? TitlePrefixes { get; set; }

        /// <summary>
        /// Available departments for dropdown
        /// </summary>
        public List<SelectListItem>? Departments { get; set; }

        /// <summary>
        /// Available phone types (Mobile, Office, Home, etc.)
        /// </summary>
        public List<SelectListItem>? PhoneTypes { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
