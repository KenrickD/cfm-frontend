namespace cfm_frontend.Models
{
    public class RelatedDocumentModel
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public int displayOrder { get; set; }
        public bool isActive { get; set; } = true;
    }
}
