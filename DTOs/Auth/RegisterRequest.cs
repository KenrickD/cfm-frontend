namespace cfm_frontend.DTOs.Auth
{
    /// <summary>
    /// Request payload for user registration endpoint
    /// </summary>
    public class RegisterRequest
    {
        public int IdUserGroup { get; set; }
        public int SalutationId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string? Title { get; set; }
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public int TimeZoneId { get; set; }
        public int CurrencyId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
