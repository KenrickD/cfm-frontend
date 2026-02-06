using cfm_frontend.Models.WorkRequest;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for the Work Request Edit page.
    /// Extends WorkRequestViewModel to include all dropdown data,
    /// plus adds the existing work request data for pre-population.
    /// </summary>
    public class WorkRequestEditViewModel : WorkRequestViewModel
    {
        /// <summary>
        /// The ID of the work request being edited
        /// </summary>
        public int IdWorkRequest { get; set; }

        /// <summary>
        /// The existing work request data to pre-populate the form
        /// </summary>
        public WorkRequestFormDetailDto? WorkRequestData { get; set; }

        /// <summary>
        /// Indicates whether this is an edit operation (vs. create)
        /// </summary>
        public bool IsEditMode => IdWorkRequest > 0;
    }
}
