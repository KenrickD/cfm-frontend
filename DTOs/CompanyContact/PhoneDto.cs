namespace cfm_frontend.DTOs.CompanyContact
{
    /// <summary>
    /// Phone number data structure for company contacts
    /// </summary>
    public class PhoneDto
    {
        /// <summary>
        /// Phone record ID (0 for new entries)
        /// </summary>
        public int IdPhone { get; set; }

        /// <summary>
        /// Phone type ID (enum: Mobile, Office, Home, etc.)
        /// </summary>
        public int IdPhoneType { get; set; }

        /// <summary>
        /// Phone type name (e.g., "Mobile", "Office")
        /// </summary>
        public string? PhoneTypeName { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this is the main/primary phone
        /// </summary>
        public bool IsMainPhone { get; set; }
    }
}
