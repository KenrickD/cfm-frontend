namespace cfm_frontend.Models
{
    public class UserInfo
    {
        public int IdWebUser { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public int PreferredClientId { get; set; }
        public int IdCompany { get; set; }
        public string TimeZoneName { get; set; } = string.Empty;
        public int PreferredTimezoneIdTimezone { get; set; } = int.MaxValue;
        public DateTime LoginTime { get; set; }
    }
}
