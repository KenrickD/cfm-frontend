using cfm_frontend.DTOs.CompanyContact;
using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Company Contact Index page with filtering and pagination
    /// </summary>
    public class CompanyContactViewModel
    {
        /// <summary>
        /// List of company contacts
        /// </summary>
        public List<CompanyContactListDto>? Contacts { get; set; }

        /// <summary>
        /// Pagination information
        /// </summary>
        public PagingInfo? Paging { get; set; }

        /// <summary>
        /// Search keyword for name or role/title
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Selected department IDs for filtering
        /// </summary>
        public List<int>? SelectedDepartments { get; set; }

        /// <summary>
        /// Show deleted data flag
        /// </summary>
        public bool ShowDeleted { get; set; }

        /// <summary>
        /// Filter options (departments list)
        /// </summary>
        public CompanyContactFilterDto? FilterOptions { get; set; }

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
