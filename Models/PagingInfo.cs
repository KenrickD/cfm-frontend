namespace cfm_frontend.Models
{
    public class PagingInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Helper method to generate page numbers for display
        public List<int> GetPageNumbers()
        {
            var pages = new List<int>();

            // If total pages is 7 or less, show all pages
            if (TotalPages <= 7)
            {
                for (int i = 1; i <= TotalPages; i++)
                {
                    pages.Add(i);
                }
                return pages;
            }

            // Always show first page
            pages.Add(1);

            // Calculate range around current page
            int rangeStart = Math.Max(2, CurrentPage - 1);
            int rangeEnd = Math.Min(TotalPages - 1, CurrentPage + 1);

            // Add ellipsis after first page if needed
            if (rangeStart > 2)
            {
                pages.Add(-1); // -1 represents ellipsis
            }

            // Add pages around current page
            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                pages.Add(i);
            }

            // Add ellipsis before last page if needed
            if (rangeEnd < TotalPages - 1)
            {
                pages.Add(-1); // -1 represents ellipsis
            }

            // Always show last page
            if (TotalPages > 1)
            {
                pages.Add(TotalPages);
            }

            return pages;
        }
    }
}
