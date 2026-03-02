using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.DTOs.CostApproverGroup;

public class CostApproverGroupPayloadDto
{
    public int IdCostApproverGroup { get; set; }

    [Required(ErrorMessage = "IdClient cannot be empty")]
    public int IdClient { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250, ErrorMessage = "Description must not exceed 250 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    public int CurrencyIdEnum { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "RangeValueStart must be greater than 0")]
    public double RangeValueStart { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "RangeValueEnd must be greater than 0")]
    public double RangeValueEnd { get; set; }

    [Required(ErrorMessage = "Properties is required")]
    [MinLength(1, ErrorMessage = "At least one Property must be selected")]
    public List<int> Properties { get; set; } = new();

    [Required(ErrorMessage = "WorkCategories is required")]
    [MinLength(1, ErrorMessage = "At least one WorkCategory must be selected")]
    public List<int> WorkCategories { get; set; } = new();

    [Required(ErrorMessage = "SubGroups is required")]
    [MinLength(1, ErrorMessage = "At least one SubGroup must be created")]
    public List<CostApproverSubGroupPayloadDto> SubGroups { get; set; } = new();
}

public class CostApproverSubGroupPayloadDto
{
    public int IdCostApproverSubGroup { get; set; }

    [Required(ErrorMessage = "Sub Group Name is required")]
    [MaxLength(100, ErrorMessage = "Sub Group Name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Approvers is required")]
    [MinLength(1, ErrorMessage = "At least one approver must be assigned")]
    public List<CostApproverGroupApproverPayloadDto> Approvers { get; set; } = new();
}

public class CostApproverGroupApproverPayloadDto
{
    [Required(ErrorMessage = "IdEmployee is required")]
    public int IdEmployee { get; set; }

    [Required(ErrorMessage = "Level is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Level must be greater than 0")]
    public int Level { get; set; }

    public bool IsPriorityApprover { get; set; }
}
