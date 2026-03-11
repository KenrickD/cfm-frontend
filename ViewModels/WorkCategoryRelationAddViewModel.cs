using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Work Category Relation Add page
    /// Contains dropdown options and property lists
    /// </summary>
    public class WorkCategoryRelationAddViewModel
    {
        /// <summary>
        /// Available work categories for dropdown
        /// </summary>
        public List<TypeFormDetailResponse> WorkCategories { get; set; } = new List<TypeFormDetailResponse>();

        /// <summary>
        /// Available priority levels for dropdown
        /// </summary>
        public List<TypeFormDetailResponse> PriorityLevels { get; set; } = new List<TypeFormDetailResponse>();

        /// <summary>
        /// Available PICs for dropdown
        /// </summary>
        public List<PICDropdownItem> PICs { get; set; } = new List<PICDropdownItem>();

        /// <summary>
        /// All properties available for client (left listbox)
        /// </summary>
        public List<PropertyItem> AllProperties { get; set; } = new List<PropertyItem>();

        /// <summary>
        /// Properties accessible by selected PIC (right listbox)
        /// Initially empty until PIC is selected
        /// </summary>
        public List<PropertyItem> AccessibleProperties { get; set; } = new List<PropertyItem>();

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }

    /// <summary>
    /// PIC dropdown item
    /// </summary>
    public class PICDropdownItem
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Property item for dual listbox
    /// </summary>
    public class PropertyItem
    {
        public int IdProperty { get; set; }
        public string PropertyName { get; set; } = string.Empty;
    }
}
