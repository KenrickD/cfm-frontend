namespace cfm_frontend.DTOs
{
    /// <summary>
    /// Base success response wrapper from backend API
    /// </summary>
    public class BaseSuccessResponse
    {
        public string msg { get; set; } = string.Empty;
        public object data { get; set; } = new();
    }

    /// <summary>
    /// Generic success response wrapper with strongly-typed data
    /// </summary>
    /// <typeparam name="T">Type of the data payload</typeparam>
    public class BaseSuccessResponse<T>
    {
        public string msg { get; set; } = string.Empty;
        public T data { get; set; } = default!;
    }

    /// <summary>
    /// Base error response wrapper from backend API
    /// </summary>
    public class BaseErrorResponse
    {
        public int errorCode { get; set; }
        public string msg { get; set; } = string.Empty;
        public object errors { get; set; } = new();
    }
}
