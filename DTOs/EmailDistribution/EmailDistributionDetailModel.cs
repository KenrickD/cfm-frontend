namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// Payload DTO for email distribution setup/edit.
    /// Matches backend EmailDistributionPayloadDto.
    /// </summary>
    public class EmailDistributionPayloadDto
    {
        public int IdEnum { get; set; }
        public int IdClient { get; set; }
        public string? PageReference { get; set; }
        public EmailSubjectPayloadDto? Subject { get; set; }
        public EmailRecipientPayloadDto? From { get; set; }
        public List<EmailRecipientPayloadDto>? To { get; set; }
        public List<EmailRecipientPayloadDto>? Cc { get; set; }
        public List<EmailRecipientPayloadDto>? Bcc { get; set; }
    }

    /// <summary>
    /// Subject configuration for email distribution.
    /// </summary>
    public class EmailSubjectPayloadDto
    {
        public int? Id { get; set; }
        public string? Text { get; set; }
    }
}
