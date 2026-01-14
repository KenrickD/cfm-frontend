namespace cfm_frontend.DTOs.Login
{
    /// <summary>
    /// Token data returned from login/refresh endpoints
    /// </summary>
    public class TokenData
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login response wrapper using BaseSuccessResponse format
    /// </summary>
    public class LoginResponse : BaseSuccessResponse<TokenData>
    {
    }
}
