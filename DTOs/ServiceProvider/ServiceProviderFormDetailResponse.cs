namespace cfm_frontend.DTOs.ServiceProvider
{
    /// <summary>
    /// Response DTO for service provider form details from backend API
    /// </summary>
    public class ServiceProviderFormDetailResponse
    {
        public int IdServiceProvider { get; set; }
        public int IdCompany { get; set; }
        public string AliasCompanyName { get; set; }
        public int SortOrder { get; set; }
    }
}
