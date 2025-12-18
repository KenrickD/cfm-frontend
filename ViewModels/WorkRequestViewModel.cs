using cfm_frontend.Models;

namespace cfm_frontend.ViewModels
{
    public class WorkRequestViewModel
    {
        public List<PropertyGroupModel>? PropertyGroups { get; set; }
        public List<WRStatusModel>? Status { get; set; }
        public List<WorkRequestResponseModel>? WorkRequest { get; set; }

        // --- Pagination Properties ---
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; } 

        // Calculated property for Total Pages
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((decimal)TotalItems / PageSize) : 0;

        // Helper booleans for UI buttons
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
