using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace APCD.Web.Models
{
    public class ApplicationUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [StringLength(200)]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "GST Number is required")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GST format")]
        [StringLength(15)]
        public string GSTNumber { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]|\\:;""'<>,.?/~`]).{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian Mobile Number")]
        [StringLength(15)]
        public string MobileNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "OEM"; // Default role

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for company profile
        public virtual CompanyProfile? CompanyProfile { get; set; }

        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
    }
}
