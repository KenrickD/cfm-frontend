namespace cfm_frontend.DTOs.Employee
{
    /// <summary>
    /// Response DTO for worker search from backend API
    /// Endpoint: /api/v1/employee/worker
    /// </summary>
    public class WorkerFormDetailResponse
    {
        public int IdEmployee { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
