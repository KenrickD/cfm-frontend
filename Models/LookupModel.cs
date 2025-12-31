namespace cfm_frontend.Models
{
    public class LookupModel
    {
        public int Id { get; set; }
        public string? Value { get; set; }
        public string? Label { get; set; }
        public int? OrderIndex { get; set; }
        public bool IsActiveData { get; set; }
    }
}
