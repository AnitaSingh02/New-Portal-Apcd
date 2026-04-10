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
        
        [StringLength(100)]
        public string State { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Country { get; set; } = "India";
        
        [StringLength(10)]
        public string PinCode { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string ContactNo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FactoryAddress { get; set; }

        [StringLength(50)]
        public string FirmType { get; set; } // Proprietary/Limited/Society/PSU

        public double? AreaSqm { get; set; }
        public int? EmployeeCount { get; set; }
        
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string FirmSize { get; set; } = string.Empty; // Micro/Small/Medium/Large

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

        public int CurrentStep { get; set; } = 1; // Tracks the resume step

        public string SelectedAPCDCategories { get; set; } = ""; // Comma-separated list for step 1

        // Official Form Points
        public string ISOStandards { get; set; } = string.Empty; // ISO 9000/14000 etc.
        public bool IsBlacklisted { get; set; }
        public string BlacklistDetails { get; set; } = string.Empty;
        public bool HasGrievanceSystem { get; set; }

        // Classification for Discounts (15%)
        public bool IsMSE { get; set; }
        public string UdyamRegistrationNo { get; set; } = string.Empty;
        public bool IsLocalSupplier { get; set; } // Class-I Local Supplier (>=50%)
        public bool IsStartup { get; set; }
        public string DPIITRecognitionNo { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<InstallationRecord> Installations { get; set; } = new List<InstallationRecord>();
        public virtual ICollection<StaffDetail> StaffDetails { get; set; } = new List<StaffDetail>();
        public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
        public virtual ICollection<ApplicationRemark> Remarks { get; set; } = new List<ApplicationRemark>();
        public virtual ICollection<TurnoverRecord> Turnovers { get; set; } = new List<TurnoverRecord>();
        public virtual ICollection<APCDCapability> Capabilities { get; set; } = new List<APCDCapability>();
        public virtual PaymentDetail? Payment { get; set; }
    }

    public class TurnoverRecord
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        
        [Required]
        public string FinancialYear { get; set; } = string.Empty; // 2022-23 etc.
        public decimal Amount { get; set; }
        public string AuditCertificatePath { get; set; } = string.Empty;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class APCDCapability
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string MainType { get; set; } // ESP, Bag Filter, Cyclones, etc.
        [Required]
        public string SubTech { get; set; } // Dry ESP, Pulse Jet, etc.
        
        public bool IsManufactured { get; set; } // SL 21
        public bool IsAppliedForEmpanelment { get; set; } // SL 22
        
        public string Category { get; set; } = string.Empty; // 1, 2, or Both
        public string DesignedCapacity { get; set; } = string.Empty; // Range
        public string TypeDetails { get; set; } = string.Empty; // For "Others" specify type

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class StaffDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string StaffType { get; set; } // Commercial / Technical
        [Required]
        public string Name { get; set; }
        [Required]
        public string Designation { get; set; }
        public string MobileNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class InstallationRecord
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string ApcdType { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string PerformanceResult { get; set; } = string.Empty;

        // Legacy/Optional fields
        public string Location { get; set; } = string.Empty;
        public DateTime? InstallationDate { get; set; }
        public string PerformanceCertPath { get; set; } = string.Empty;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    // Standard Application Tables remains similar
    public class ApplicationRemark
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class ApplicationDocument
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public bool IsVerified { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public virtual EmpanelmentApplication Application { get; set; }
    }

    public class PaymentDetail
    {
        [Key]
        [ForeignKey("Application")]
        public int ApplicationId { get; set; }
        public decimal Amount { get; set; }
        public string UTRNumber { get; set; }
        public string RemitterBank { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "Pending";

        // Application Fees Details
        public decimal AppFeeAmountDeposited { get; set; }
        public string AppFeeRemitterBank { get; set; } = string.Empty;
        public string AppFeeUTRNumber { get; set; } = string.Empty;
        public DateTime? AppFeePaymentDate { get; set; }

        // Empanelment Fees Details
        public int APCDTypesCount { get; set; }
        public decimal EmpFeeAmountDeposited { get; set; }
        public string EmpFeeRemitterBank { get; set; } = string.Empty;
        public string EmpFeeUTRNumber { get; set; } = string.Empty;
        public DateTime? EmpFeePaymentDate { get; set; }

        public virtual EmpanelmentApplication Application { get; set; }
    }
}
