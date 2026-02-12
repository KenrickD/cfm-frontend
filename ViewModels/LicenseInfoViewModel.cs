namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel to store validated license information in session
    /// </summary>
    public class LicenseInfoViewModel
    {
        public int IdUserGroup { get; set; }
        public int IdClient { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ClientLogoUrl { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string? RequiredEmailDomain { get; set; }
        public int? QuotaRemaining { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
