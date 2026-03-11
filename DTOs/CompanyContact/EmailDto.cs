namespace cfm_frontend.DTOs.CompanyContact
{
    /// <summary>
    /// Email address data structure for company contacts
    /// </summary>
    public class EmailDto
    {
        /// <summary>
        /// Email record ID (0 for new entries)
        /// </summary>
        public int IdEmail { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this is the main/primary email
        /// </summary>
        public bool IsMainEmail { get; set; }
    }
}
