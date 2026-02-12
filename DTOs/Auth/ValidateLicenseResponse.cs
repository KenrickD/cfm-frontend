namespace cfm_frontend.DTOs.Auth
{
    /// <summary>
    /// Response data from license validation
    /// </summary>
    public class ValidateLicenseData
    {
        public int IdUserGroup { get; set; }
        public int IdClient { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ClientLogoUrl { get; set; }
        public string? RequiredEmailDomain { get; set; }
        public int? QuotaRemaining { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Response wrapper for license validation endpoint
    /// </summary>
    public class ValidateLicenseResponse : ApiResponseDto<ValidateLicenseData>
    {
    }
}
