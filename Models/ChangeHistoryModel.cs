namespace cfm_frontend.Models
{
    public class ChangeHistoryModel
    {
        public int Id { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ChangedInformation { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
    }
}
