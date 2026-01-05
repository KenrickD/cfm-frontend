namespace cfm_frontend.Models
{
    public class OfficeHourModel
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public int OfficeDay { get; set; }  // 0=Sunday, 1=Monday, ..., 6=Saturday
        public TimeSpan FromHour { get; set; }
        public TimeSpan ToHour { get; set; }
        public bool IsWorkingHour { get; set; }
        public bool IsActiveData { get; set; }
    }
}
