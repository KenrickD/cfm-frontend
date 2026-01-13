namespace cfm_frontend.DTOs.UserInfo
{
    public class UserInfoResponse
    {
        public int IdWebUser { get; set; }
        //public string Username { get; set; } = string.Empty;
        //public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string TimeZoneName {  get; set; } = string.Empty;
        //public string Role { get; set; } = string.Empty;
        //public string Department { get; set; } = string.Empty;
        //public string PhoneNumber { get; set; } = string.Empty;
        //public string ProfilePicture { get; set; } = string.Empty;
        public int Preferred_Client_idClient { get; set; }
        public int Preferred_TimeZone_idTimeZone { get; set; }
        public int Preferred_Company_idCompany { get; set; }
    }
}
