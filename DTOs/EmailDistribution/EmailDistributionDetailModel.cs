namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// DTO for email distribution detail (setup/edit)
    /// </summary>
    public class EmailDistributionDetailModel
    {
        public int? IdEmailDistributionList { get; set; }
        public string PageReference { get; set; } = string.Empty;
        public string SubjectType { get; set; } = "default";
        public string? CustomSubject { get; set; }
        public string FromType { get; set; } = "default";
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public List<EmailRecipientModel> Recipients { get; set; } = new();
    }
}
