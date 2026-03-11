using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for Work Category Relation Edit page
    /// Extends Add view model with pre-selected values
    /// </summary>
    public class WorkCategoryRelationEditViewModel
    {
        public int IdWorkCategoryRelation { get; set; }

        /// <summary>
        /// Selected work category ID
        /// </summary>
        public int SelectedWorkCategoryId { get; set; }

        /// <summary>
        /// Selected priority level ID
        /// </summary>
        public int SelectedPriorityLevelId { get; set; }

        /// <summary>
        /// Selected PIC ID
        /// </summary>
        public int SelectedPICId { get; set; }

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
        /// Properties already assigned to this relation (right listbox)
        /// </summary>
        public List<PropertyItem> AssignedProperties { get; set; } = new List<PropertyItem>();

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety
        /// </summary>
        public int IdClient { get; set; }
    }
}
