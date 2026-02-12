using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for license key validation form
    /// </summary>
    public class LicenseValidationViewModel
    {
        [Required(ErrorMessage = "License key part 1 is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Must be 4 characters")]
        [RegularExpression(@"^[A-Z0-9]{4}$", ErrorMessage = "Must be 4 alphanumeric characters")]
        public string Key1 { get; set; } = string.Empty;

        [Required(ErrorMessage = "License key part 2 is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Must be 4 characters")]
        [RegularExpression(@"^[A-Z0-9]{4}$", ErrorMessage = "Must be 4 alphanumeric characters")]
        public string Key2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "License key part 3 is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Must be 4 characters")]
        [RegularExpression(@"^[A-Z0-9]{4}$", ErrorMessage = "Must be 4 alphanumeric characters")]
        public string Key3 { get; set; } = string.Empty;

        [Required(ErrorMessage = "License key part 4 is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Must be 4 characters")]
        [RegularExpression(@"^[A-Z0-9]{4}$", ErrorMessage = "Must be 4 alphanumeric characters")]
        public string Key4 { get; set; } = string.Empty;

        [Required(ErrorMessage = "License password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "License Password")]
        public string LicensePassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets the combined 16-character license key
        /// </summary>
        public string FullLicenseKey => $"{Key1}{Key2}{Key3}{Key4}".ToUpperInvariant();
    }
}
