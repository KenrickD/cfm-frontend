namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// DTO for email distribution list item.
    /// Matches backend EmailDistributionViewDTO.
    /// </summary>
    public class EmailDistributionViewDto
    {
        public int IdEnum { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PageReference { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public bool CanSetup { get; set; }
    }
}
