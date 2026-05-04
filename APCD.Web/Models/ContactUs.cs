using System;
using System.ComponentModel.DataAnnotations;

namespace APCD.Web.Models
{
    public class ContactUs
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid Phone Number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be 10 digits")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must be numeric")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Summary is required")]
        [StringLength(2500, ErrorMessage = "Summary cannot exceed 500 words")] // Approx 500 words
        public string Summary { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
