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

        [StringLength(50)]
        public string? ApplicationId { get; set; }

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
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<SupplementalRequest> SupplementalRequests { get; set; } = new List<SupplementalRequest>();
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

        public bool IsPaid { get; set; } = false;
        public int? PaymentId { get; set; }

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

        // New Field Verification Tracking
        public string VerificationStatus { get; set; } = "Pending"; // Pending, Completed
        public string? VerifiedBy { get; set; }
        public DateTime? VerificationDate { get; set; }
        public bool IsCertificateIssued { get; set; } = false;

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
        public string AssociatedTech { get; set; } = string.Empty; // Grouping by Tech for Step 4
        public bool IsVerified { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Rejection tracking
        public bool IsRejected { get; set; }
        public string RejectionType { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime? RejectedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Versioning & Audit
        public int? ParentDocumentId { get; set; }
        public int Version { get; set; } = 1;
        public string DocumentStatus { get; set; } = "Pending"; // Pending, Verified, Rejected
        public bool IsActive { get; set; } = true;

        // Categorization
        public int StepNumber { get; set; } = 0;
        public string DocumentCategory { get; set; } = "Common"; // Common, APCD


        public virtual EmpanelmentApplication Application { get; set; }
        public virtual ICollection<DocumentReviewHistory> ReviewHistories { get; set; } = new List<DocumentReviewHistory>();
    }

    public class DocumentReviewHistory
    {
        [Key]
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Status { get; set; } // Pending, Verified, Rejected, Re-uploaded
        public string RejectionType { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public string ActionBy { get; set; }
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationDocument Document { get; set; }
    }

    public enum PaymentType
    {
        AppFee,
        EmpFee,
        Supplemental
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Application")]
        public int ApplicationId { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public PaymentType? Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [StringLength(100)]
        public string? UTRNumber { get; set; }

        [StringLength(200)]
        public string? RemitterBank { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? ReceiptPath { get; set; }

        // Contextual metric: How many APCD units does this specific payment cover?
        public int? APCDTypesCount { get; set; } 
        public bool IsSupplemental { get; set; } = false;

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual EmpanelmentApplication Application { get; set; }
    }

    [NotMapped]
    public class PaymentViewModel
    {
        public int ApplicationId { get; set; }
        public decimal Amount { get; set; }
        public int APCDTypesCount { get; set; }

        // Application Fees Details
        public decimal AppFeeAmountDeposited { get; set; }
        public string AppFeeRemitterBank { get; set; } = string.Empty;
        public string AppFeeUTRNumber { get; set; } = string.Empty;
        public DateTime? AppFeePaymentDate { get; set; }

        // Empanelment Fees Details
        public decimal EmpFeeAmountDeposited { get; set; }
        public string EmpFeeRemitterBank { get; set; } = string.Empty;
        public string EmpFeeUTRNumber { get; set; } = string.Empty;
        public DateTime? EmpFeePaymentDate { get; set; }

        // Supplemental / Amendment Fees
        public decimal? SupplementalAmount { get; set; }
        public string SupplementalUTR { get; set; } = string.Empty;
        public string SupplementalBankName { get; set; } = string.Empty;
        public decimal? SupplementalAmountDeposited { get; set; }
        public string SupplementalReceiptPath { get; set; } = string.Empty;
        public DateTime? SupplementalPayDate { get; set; }
        
        public EmpanelmentApplication Application { get; set; }
    }

    public class SupplementalRequest
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int UserId { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
        public bool IsFinalSubmitted { get; set; } = false;
        public int LastCompletedStep { get; set; } = 4; // Supplemental flow starts at 4
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinalSubmittedAt { get; set; }

        public virtual EmpanelmentApplication Application { get; set; }
        public virtual ICollection<SupplementalDevice> Devices { get; set; } = new List<SupplementalDevice>();
        public virtual ICollection<SupplementalDocument> Documents { get; set; } = new List<SupplementalDocument>();
        public virtual ICollection<SupplementalPayment> Payments { get; set; } = new List<SupplementalPayment>();
    }

    public class SupplementalDevice
    {
        [Key]
        public int Id { get; set; }
        public int SupplementalRequestId { get; set; }
        public string MainType { get; set; } = string.Empty;
        public string SubTech { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DesignedCapacity { get; set; } = string.Empty;
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public virtual SupplementalRequest SupplementalRequest { get; set; }
    }

    public class SupplementalDocument
    {
        [Key]
        public int Id { get; set; }
        public int SupplementalRequestId { get; set; }
        public string APCDType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        public virtual SupplementalRequest SupplementalRequest { get; set; }
    }

    public class SupplementalPayment
    {
        [Key]
        public int Id { get; set; }
        public int SupplementalRequestId { get; set; }
        public decimal Amount { get; set; }
        public decimal GST { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountDeposited { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string UTRNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string ReceiptPath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        
        // Metadata for tracking
        public int NewlyAddedAPCDCount { get; set; }
        public string NewlyAddedAPCDTypes { get; set; } = string.Empty; // Comma separated list
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual SupplementalRequest SupplementalRequest { get; set; }
    }

    public class SupplementalTransactionHistory
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int SupplementalRequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
