namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// Paginated response wrapper for email distribution list.
    /// </summary>
    public class EmailDistributionPagedResponse
    {
        public List<EmailDistributionViewDto>? Data { get; set; }
        public EmailDistributionPagingMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Pagination metadata for email distribution list responses.
    /// </summary>
    public class EmailDistributionPagingMetadata
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
