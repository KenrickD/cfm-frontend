namespace cfm_frontend.DTOs.CompanyContact
{
    /// <summary>
    /// Filter options for company contact list page
    /// </summary>
    public class CompanyContactFilterDto
    {
        /// <summary>
        /// Available departments for filtering
        /// </summary>
        public List<DepartmentFilterItem>? Departments { get; set; }
    }

    /// <summary>
    /// Department filter item
    /// </summary>
    public class DepartmentFilterItem
    {
        /// <summary>
        /// Department ID
        /// </summary>
        public int IdDepartment { get; set; }

        /// <summary>
        /// Department name
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Count of contacts in this department
        /// </summary>
        public int ContactCount { get; set; }
    }
}
