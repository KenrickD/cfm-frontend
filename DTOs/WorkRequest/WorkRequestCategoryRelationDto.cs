using System.Collections.Generic;

namespace cfm_frontend.DTOs.WorkRequest
{
    /// <summary>
    /// DTO for Work Request Category Relation auto-binding
    /// Maps backend WorkRequestCategoryRelationResponse model
    /// Used to auto-select PIC and Priority Level based on Work Category + Location
    /// </summary>
    public class WorkRequestCategoryRelationDto
    {
        /// <summary>
        /// Unique identifier for the category relation
        /// </summary>
        public int IdWorkRequestCategory_Relation { get; set; }

        /// <summary>
        /// Work Category Type ID (matches dropdown value)
        /// </summary>
        public int WorkCategory_Type_idType { get; set; }

        /// <summary>
        /// Priority Level ID to auto-select
        /// </summary>
        public int PriorityLevel_idPriorityLevel { get; set; }

        /// <summary>
        /// Person in Charge Employee ID to auto-select
        /// </summary>
        public int Pic_Employe_idEmployee { get; set; }

        /// <summary>
        /// List of Property IDs this relation applies to
        /// If selected location matches any ID in this list, auto-binding applies
        /// </summary>
        public List<int> Property_idProperty { get; set; } = new List<int>();
    }
}
