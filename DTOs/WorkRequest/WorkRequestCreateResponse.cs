namespace cfm_frontend.DTOs.WorkRequest
{
    public class WorkRequestCreateResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int idWorkRequest { get; set; }
        public string workRequestCode { get; set; }
    }
}
