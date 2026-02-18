namespace cfm_frontend.DTOs.Masters;

public class CompanyContactInfoDto
{
    public int IdEmployee { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
