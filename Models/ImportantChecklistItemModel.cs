namespace cfm_frontend.Models
{
    public class ImportantChecklistItemModel
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public int displayOrder { get; set; }
        public bool isActive { get; set; } = true;
    }

    public class ImportantChecklistUpdateOrderRequest
    {
        public List<ImportantChecklistOrderItem> items { get; set; } = new List<ImportantChecklistOrderItem>();
    }

    public class ImportantChecklistOrderItem
    {
        public int id { get; set; }
        public int displayOrder { get; set; }
    }
}
