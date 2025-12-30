namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// DTO for email recipient
    /// </summary>
    public class EmailRecipientModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Type { get; set; } = "TO"; // TO, CC, BCC
    }
}
