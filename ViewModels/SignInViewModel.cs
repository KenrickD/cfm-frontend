using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.ViewModels
{
    public class SignInViewModel
    {
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string? Username { get; set; }

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [StringLength(250, ErrorMessage = "Password cannot exceed 250 characters")]
        public string? Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        

        [DataType(DataType.Password)]
        [StringLength(250, ErrorMessage = "License password cannot exceed 250 characters")]
        public string? LicensePassword { get; set; }

        public bool IsSignUp { get; set; }

        
    }
}