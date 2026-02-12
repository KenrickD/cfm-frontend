namespace cfm_frontend.DTOs.Auth
{
    /// <summary>
    /// Request payload for license validation endpoint
    /// </summary>
    public class ValidateLicenseRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string LicensePassword { get; set; } = string.Empty;
    }
}
