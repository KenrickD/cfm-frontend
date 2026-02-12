using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.ViewModels
{
    /// <summary>
    /// ViewModel for registration form
    /// </summary>
    public class RegisterViewModel
    {
        // License info (read-only, from session)
        public LicenseInfoViewModel? LicenseInfo { get; set; }

        // Dropdown options (populated from API)
        public List<SelectListItemModel> Salutations { get; set; } = new();
        public List<SelectListItemModel> Departments { get; set; } = new();
        public List<SelectListItemModel> TimeZones { get; set; } = new();
        public List<SelectListItemModel> Currencies { get; set; } = new();

        // Form fields
        [Required(ErrorMessage = "Salutation is required")]
        [Display(Name = "Salutation")]
        public int SalutationId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Display(Name = "Role/Title")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string? Title { get; set; }

        [Display(Name = "Mobile Phone")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Time zone is required")]
        [Display(Name = "Preferred Time Zone")]
        public int TimeZoneId { get; set; }

        [Required(ErrorMessage = "Currency is required")]
        [Display(Name = "Preferred Currency")]
        public int CurrencyId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simple model for dropdown select items
    /// </summary>
    public class SelectListItemModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
