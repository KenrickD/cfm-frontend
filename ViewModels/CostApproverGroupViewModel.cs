using cfm_frontend.DTOs.CostApproverGroup;

namespace cfm_frontend.ViewModels;

public class CostApproverGroupViewModel
{
    public List<CostApproverGroupListDto>? Items { get; set; }
    public int IdClient { get; set; }
}
