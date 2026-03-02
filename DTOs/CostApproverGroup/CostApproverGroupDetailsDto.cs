namespace cfm_frontend.DTOs.CostApproverGroup;

public class CostApproverGroupDetailsDto
{
    public int IdCostApproverGroup { get; set; }
    public int IdClient { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CostApproverGroupRangeValueDto RangeValue { get; set; } = new();
    public List<CostApproverGroupPropertyDto> Properties { get; set; } = new();
    public List<CostApproverGroupWorkCategoryDto> WorkCategories { get; set; } = new();
    public List<CostApproverGroupSubGroupDto> SubGroups { get; set; } = new();
}

public class CostApproverGroupRangeValueDto
{
    public int Currency_IdEnum { get; set; }
    public string Currency { get; set; } = string.Empty;
    public double AmountStart { get; set; }
    public double AmountEnd { get; set; }
}

public class CostApproverGroupPropertyDto
{
    public int IdCostApproverGroupProperty { get; set; }
    public int IdProperty { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CostApproverGroupWorkCategoryDto
{
    public int IdCostApproverGroupWorkCategory { get; set; }
    public int WorkCategory_IdType { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CostApproverGroupSubGroupDto
{
    public int IdCostApproverSubGroup { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CostApproverGroupLevelDto> Levels { get; set; } = new();
}

public class CostApproverGroupLevelDto
{
    public int Level { get; set; }
    public List<CostApproverGroupApproverDto> Approvers { get; set; } = new();
}

public class CostApproverGroupApproverDto
{
    public int IdCostApproverGroupApprover { get; set; }
    public int IdEmployee { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsPriorityApprover { get; set; }
}
