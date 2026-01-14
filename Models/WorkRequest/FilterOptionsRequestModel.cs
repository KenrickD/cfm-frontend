namespace cfm_frontend.Models.WorkRequest
{
    /// <summary>
    /// Request model for getting filter options from backend API
    /// </summary>
    public class FilterOptionsRequestModel
    {
        /// <summary>
        /// Client ID from user session
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Search keywords from main search box (optional)
        /// Backend may use this to filter returned options
        /// </summary>
        public string Keywords { get; set; } = string.Empty;
    }
}
