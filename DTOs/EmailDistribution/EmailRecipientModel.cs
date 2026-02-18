namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// Recipient DTO for email distribution.
    /// Matches backend EmailRecipientPayloadtDTO.
    /// </summary>
    public class EmailRecipientPayloadDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
