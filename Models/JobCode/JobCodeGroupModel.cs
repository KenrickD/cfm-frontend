namespace cfm_frontend.Models.JobCode
{
    public class JobCodeGroupModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int IdClient { get; set; }
        public bool IsActiveData { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
