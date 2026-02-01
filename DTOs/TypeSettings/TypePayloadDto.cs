using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.DTOs.TypeSettings
{
    /// <summary>
    /// Shared DTO for Type-based category create/update operations.
    /// Used by WorkCategory, OtherCategory, OtherCategory2.
    /// Maps to backend Type_PayloadDto structure.
    /// </summary>
    public class TypePayloadDto
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
