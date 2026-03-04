namespace cfm_frontend.DTOs
{
    /// <summary>
    /// Paginated response structure matching backend PagedResponse
    /// </summary>
    public class PagedResponse<T>
    {
        public IEnumerable<T>? Data { get; set; }
        public Metadata? Metadata { get; set; }
    }

    /// <summary>
    /// Pagination metadata matching backend Metadata
    /// </summary>
    public class Metadata
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
