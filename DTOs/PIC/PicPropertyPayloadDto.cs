namespace cfm_frontend.DTOs.PIC
{
    /// <summary>
    /// Payload DTO for creating/updating PIC property assignments.
    /// Used by POST and PUT /api/v1/work-request/pic
    /// </summary>
    public class PicPropertyPayloadDto
    {
        public int IdEmployee { get; set; }
        public int IdClient { get; set; }
        public int[] AssignedProperties { get; set; } = [];
        public int[] UnassignedProperties { get; set; } = [];
    }
}
