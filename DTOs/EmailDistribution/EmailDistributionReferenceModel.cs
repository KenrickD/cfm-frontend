namespace cfm_frontend.DTOs.EmailDistribution
{
    /// <summary>
    /// DTO for email distribution page reference list display
    /// Represents each email distribution type with its configuration status
    /// </summary>
    public class EmailDistributionReferenceModel
    {
        /// <summary>
        /// Enum ID from the Enum table
        /// </summary>
        public int IdEnum { get; set; }

        /// <summary>
        /// Display text for the distribution type
        /// Example: "HelpdeskNotificationEmail"
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Value/key for the distribution type
        /// Example: "HelpdeskNotificationEmail"
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Flag indicating whether a distribution list exists for this type and client
        /// True = Show "Edit" button, False = Show "Set Up" button
        /// </summary>
        public bool HasDistributionList { get; set; }

        /// <summary>
        /// Distribution list ID if it exists, null otherwise
        /// Used for Edit and Delete operations
        /// </summary>
        public int? DistributionListId { get; set; }
    }
}
