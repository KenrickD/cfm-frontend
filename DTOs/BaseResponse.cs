namespace cfm_frontend.DTOs
{
    /// <summary>
    /// Unified API response wrapper for all backend API responses.
    /// </summary>
    /// <typeparam name="T">Type of the data payload</typeparam>
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
