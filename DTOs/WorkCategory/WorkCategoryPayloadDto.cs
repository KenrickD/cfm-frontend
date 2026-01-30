using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.DTOs.WorkCategory
{
    /// <summary>
    /// DTO for Work Category create/update operations.
    /// Maps to backend Type_PayloadDto structure.
    /// </summary>
    public class WorkCategoryPayloadDto
    {
        public int IdType { get; set; }
        public int? Parent_idType { get; set; }

        [Required(ErrorMessage = "IdClient is required")]
        public int IdClient { get; set; }

        public string? Category { get; set; }
        public int? DisplayOrder { get; set; }

        [Required(ErrorMessage = "Text is required")]
        public string Text { get; set; } = string.Empty;
    }
}
