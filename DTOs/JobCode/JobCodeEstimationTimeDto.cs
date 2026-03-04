namespace cfm_frontend.DTOs.JobCode
{
    /// <summary>
    /// Estimation time structure for job codes
    /// Maps to backend JobCodeEstimationTimeDto
    /// </summary>
    public class JobCodeEstimationTimeDto
    {
        public long? TimeSpan { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
    }
}
