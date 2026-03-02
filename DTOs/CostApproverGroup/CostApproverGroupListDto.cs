namespace cfm_frontend.DTOs.CostApproverGroup;

public class CostApproverGroupListDto
{
    public int IdCostApproverGroup { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Properties { get; set; } = new();
    public List<string> WorkCategories { get; set; } = new();
    public List<string> Flows { get; set; } = new();
    public string Currency { get; set; } = string.Empty;
    public double RangeValueStart { get; set; }
    public double RangeValueEnd { get; set; }
}

public class CostApproverGroupPagedResponse
{
    public List<CostApproverGroupListDto>? Data { get; set; }
    public CostApproverGroupPagingMetadata? Metadata { get; set; }
}

public class CostApproverGroupPagingMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
