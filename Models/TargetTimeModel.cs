namespace cfm_frontend.Models
{
    public class TargetTimeModel
    {
        public DateTime? originalTarget { get; set; }
        public DateTime? newTarget { get; set; }
        public string remark { get; set; }
        public string targetType { get; set; } // helpdesk, initialFollowUp, quotation, etc.
    }
}
