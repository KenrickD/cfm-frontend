namespace cfm_frontend.DTOs.CompanyContact
{
    /// <summary>
    /// Company contact list item for Index page display
    /// </summary>
    public class CompanyContactListDto
    {
        /// <summary>
        /// Contact ID
        /// </summary>
        public int IdContact { get; set; }

        /// <summary>
        /// Contact full name with title
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// Department name
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Role or job title
        /// </summary>
        public string? RoleTitle { get; set; }

        /// <summary>
        /// Primary phone number display
        /// </summary>
        public string? PhoneDisplay { get; set; }

        /// <summary>
        /// Primary email address
        /// </summary>
        public string? EmailDisplay { get; set; }

        /// <summary>
        /// Indicates if contact has a user account
        /// </summary>
        public bool HasUserAccount { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
