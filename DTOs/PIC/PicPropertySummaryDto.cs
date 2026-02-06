namespace cfm_frontend.DTOs.PIC
{
    /// <summary>
    /// Summary DTO for PIC list items.
    /// Returned by GET /api/v1/work-request/pic/list
    /// </summary>
    public class PicPropertySummaryDto
    {
        public int IdEmployee { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int IdClient { get; set; }
        public int TotalAssignedProperties { get; set; }
        public int TotalProperties { get; set; }
    }
}
