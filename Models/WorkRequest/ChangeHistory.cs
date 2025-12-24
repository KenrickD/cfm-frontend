namespace cfm_frontend.Models.WorkRequest
{
    public class ChangeHistory
    {
        public int Version { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public string ChangedInformation { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
