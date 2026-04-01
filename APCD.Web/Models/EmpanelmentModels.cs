using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APCD.Web.Models
{
    public class CompanyProfile
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; }
        
        [Required]
        [StringLength(15)]
        public string GSTNumber { get; set; }
        
        [Required]
        [StringLength(10)]
        public string PANNumber { get; set; }
        
        [Required]
        [StringLength(500)]
        public string OfficeAddress { get; set; }
        
        [Required]
        [StringLength(500)]
        public string FactoryAddress { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class EmpanelmentApplication
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Submitted, PendingVerification, Provisional, Final, Rejected

        public string SelectedAPCDCategories { get; set; } = ""; // Comma-separated list

        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<InstallationRecord> Installations { get; set; } = new List<InstallationRecord>();
        public virtual ICollection<StaffDetail> StaffDetails { get; set; } = new List<StaffDetail>();
        public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
        public virtual ICollection<ApplicationRemark> Remarks { get; set; } = new List<ApplicationRemark>();
        public virtual PaymentDetail? Payment { get; set; }
    }

    public class ApplicationRemark
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        
        [Required]
        public string Role { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class InstallationRecord
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string ClientName { get; set; }
        [Required]
        public string Location { get; set; }
        public DateTime InstallationDate { get; set; }

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class StaffDetail
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public string Designation { get; set; }
        public string Qualification { get; set; }

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class ApplicationDocument
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string DocumentType { get; set; } // CompanyPAN, GST, ISO, TechSpec, etc.
        [Required]
        public string FileName { get; set; }
        [Required]
        public string FilePath { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class PaymentDetail
    {
        [Key]
        [ForeignKey("Application")]
        public int ApplicationId { get; set; }

        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(100)]
        public string UTRNumber { get; set; }
        
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Verified, Failed

        public virtual EmpanelmentApplication Application { get; set; }
    }
}
