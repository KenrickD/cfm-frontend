namespace cfm_frontend.DTOs.CompanyContact
{
    /// <summary>
    /// Complete company contact data structure for Add/Edit/Detail operations
    /// </summary>
    public class CompanyContactDto
    {
        /// <summary>
        /// Contact ID (0 for new contacts)
        /// </summary>
        public int IdContact { get; set; }

        /// <summary>
        /// Client ID
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Title prefix (e.g., Mr, Ms, Mrs)
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Contact full name
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// Department ID
        /// </summary>
        public int? IdDepartment { get; set; }

        /// <summary>
        /// Department name
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Role or job title
        /// </summary>
        public string? RoleTitle { get; set; }

        /// <summary>
        /// Additional notes about the contact
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indicates if contact has a user account
        /// </summary>
        public bool HasUserAccount { get; set; }

        /// <summary>
        /// List of phone numbers
        /// </summary>
        public List<PhoneDto>? Phones { get; set; }

        /// <summary>
        /// List of email addresses
        /// </summary>
        public List<EmailDto>? Emails { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
