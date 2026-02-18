namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for the Email Distribution setup/edit detail page.
    /// </summary>
    public class EmailDistributionDetailViewModel
    {
        /// <summary>
        /// Enum ID identifying which email distribution type to configure.
        /// </summary>
        public int IdEnum { get; set; }

        /// <summary>
        /// Human-readable page reference name for display.
        /// </summary>
        public string PageReference { get; set; } = string.Empty;

        /// <summary>
        /// Page mode: "setup" for new configuration, "edit" for existing.
        /// </summary>
        public string Mode { get; set; } = "setup";

        /// <summary>
        /// Client ID captured at page load for multi-tab session safety.
        /// </summary>
        public int IdClient { get; set; }
    }
}
