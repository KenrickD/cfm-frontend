namespace cfm_frontend.DTOs.Auth
{
    /// <summary>
    /// Request payload for forgot password endpoint
    /// </summary>
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
