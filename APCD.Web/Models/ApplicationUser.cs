using System;
using System.ComponentModel.DataAnnotations;

namespace APCD.Web.Models
{
    public class ApplicationUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(15)]
        public string MobileNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "OEM"; // Default role

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for company profile
        public virtual CompanyProfile? CompanyProfile { get; set; }
    }
}
