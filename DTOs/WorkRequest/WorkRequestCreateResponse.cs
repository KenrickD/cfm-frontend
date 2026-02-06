namespace cfm_frontend.DTOs.WorkRequest
{
    public class WorkRequestCreateResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int idWorkRequest { get; set; }
        public string workRequestCode { get; set; }
    }

    /// <summary>
    /// Data payload returned by the Work Request Create API endpoint.
    /// Used as T in ApiResponseDto&lt;WorkRequestCreateData&gt;.
    /// </summary>
    public class WorkRequestCreateData
    {
        public int IdWorkRequest { get; set; }
    }
}
