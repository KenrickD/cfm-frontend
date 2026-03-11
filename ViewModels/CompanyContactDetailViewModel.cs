using cfm_frontend.DTOs.CompanyContact;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Company Contact Detail page (read-only view)
    /// </summary>
    public class CompanyContactDetailViewModel
    {
        /// <summary>
        /// Complete contact information
        /// </summary>
        public CompanyContactDto? Contact { get; set; }

        /// <summary>
        /// Client ID for navigation back to list
        /// </summary>
        public int IdClient { get; set; }
    }
}
