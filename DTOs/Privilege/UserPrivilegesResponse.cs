using System.Text.Json.Serialization;

namespace cfm_frontend.DTOs.Privilege
{
    /// <summary>
    /// API response DTO for user privileges
    /// </summary>
    public class UserPrivilegesResponse
    {
        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; } = string.Empty;

        [JsonPropertyName("pages")]
        public List<PagePrivilegeDto> Pages { get; set; } = new List<PagePrivilegeDto>();
    }

    /// <summary>
    /// Page privilege DTO matching backend API response
    /// </summary>
    public class PagePrivilegeDto
    {
        [JsonPropertyName("pageName")]
        public string PageName { get; set; } = string.Empty;

        [JsonPropertyName("canView")]
        public bool CanView { get; set; }

        [JsonPropertyName("canAdd")]
        public bool CanAdd { get; set; }

        [JsonPropertyName("canEdit")]
        public bool CanEdit { get; set; }

        [JsonPropertyName("canDelete")]
        public bool CanDelete { get; set; }
    }
}
