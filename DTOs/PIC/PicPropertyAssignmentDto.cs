namespace cfm_frontend.DTOs.PIC
{
    /// <summary>
    /// Detail DTO for PIC property assignment.
    /// Returned by GET /api/v1/work-request/pic/{employeeId}?cid={clientId}
    /// </summary>
    public class PicPropertyAssignmentDto
    {
        public int IdEmployee { get; set; }
        public string? FullName { get; set; }
        public int IdClient { get; set; }
        public List<PicPropertyItemDto>? AvailableProperties { get; set; }
        public List<PicPropertyItemDto>? AssignedProperties { get; set; }
    }

    /// <summary>
    /// Individual property item in PIC assignment lists.
    /// </summary>
    public class PicPropertyItemDto
    {
        public int IdProperty { get; set; }
        public string? PropertyName { get; set; }
    }
}
