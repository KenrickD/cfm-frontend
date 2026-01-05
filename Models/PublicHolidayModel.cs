namespace cfm_frontend.Models
{
    public class PublicHolidayModel
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActiveData { get; set; }
    }
}
