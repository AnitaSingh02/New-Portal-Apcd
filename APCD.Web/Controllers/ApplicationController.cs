using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APCD.Web.Models;
using APCD.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APCD.Web.Controllers
{
    [Authorize(Roles = "OEM")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ApplicationController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            
            // Reject attempts to start new forms if they have an active running application
            var existingActive = await _context.Applications
                .Where(a => a.UserId == userId && a.Status != "Rejected")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingActive != null && existingActive.Status != "Draft")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var application = existingActive;

            if (application == null)
            {
                application = new EmpanelmentApplication { UserId = userId, Status = "Draft", CurrentStep = 1 };
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Resume", new { id = application.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Resume(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return RedirectToAction("Index", "Dashboard");
            if (application.Status != "Draft") return RedirectToAction("Review", new { id });

            return application.CurrentStep switch
            {
                2 => RedirectToAction("Step2", new { id }),
                3 => RedirectToAction("Step3", new { id }),
                4 => RedirectToAction("Step4", new { id }),
                5 => RedirectToAction("Step5", new { id }),
                6 => RedirectToAction("Review", new { id }),
                _ => RedirectToAction("Step1", new { id })
            };
        }

        #region Step 1: Company Profile (Points 1-6, 9-13)
        [HttpGet]
        public async Task<IActionResult> Step1(int id)
        {
            var userId = GetUserId();
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            ViewBag.AppId = id;
            return View(profile ?? new CompanyProfile { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> Step1(int id, CompanyProfile profile)
        {
            var userId = GetUserId();
            profile.UserId = userId;
            profile.UpdatedAt = DateTime.UtcNow;
            
            // Ensure strings are not null
            profile.CompanyName = profile.CompanyName ?? string.Empty;
            profile.GSTNumber = profile.GSTNumber ?? string.Empty;
            profile.PANNumber = profile.PANNumber ?? string.Empty;
            profile.OfficeAddress = profile.OfficeAddress ?? string.Empty;
            profile.FactoryAddress = profile.FactoryAddress ?? string.Empty;
            profile.State = profile.State ?? string.Empty;
            profile.PinCode = profile.PinCode ?? string.Empty;
            profile.ContactNo = profile.ContactNo ?? string.Empty;
            profile.FirmType = profile.FirmType ?? string.Empty;
            profile.FirmSize = profile.FirmSize ?? string.Empty;
            profile.Latitude = profile.Latitude ?? string.Empty;
            profile.Longitude = profile.Longitude ?? string.Empty;

            if (await _context.CompanyProfiles.AnyAsync(p => p.UserId == userId))
                _context.CompanyProfiles.Update(profile);
            else
                _context.CompanyProfiles.Add(profile);

            var app = await _context.Applications.FindAsync(id);
            if (app != null) {
                app.CurrentStep = Math.Max(app.CurrentStep, 2);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Step2", new { id });
        }
        #endregion

        #region Step 2: Classifications (Points 7, 8, 14, 19, 20)
        [HttpGet]
        public async Task<IActionResult> Step2(int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .ThenInclude(u => u.CompanyProfile)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();
            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> Step2(int id, EmpanelmentApplication model)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            application.ISOStandards = model.ISOStandards ?? string.Empty;
            application.IsBlacklisted = model.IsBlacklisted;
            application.BlacklistDetails = model.BlacklistDetails ?? string.Empty;
            application.IsMSE = model.IsMSE;
            application.UdyamRegistrationNo = model.UdyamRegistrationNo ?? string.Empty;
            application.IsLocalSupplier = model.IsLocalSupplier;
            application.IsStartup = model.IsStartup;
            application.DPIITRecognitionNo = model.DPIITRecognitionNo ?? string.Empty;
            application.CurrentStep = Math.Max(application.CurrentStep, 3);

            string oemFolder = await GetOEMFolderName(id);

            await ProcessFileUpload(id, "isoStandardsFile", "ISOStandardsCertificate", oemFolder);
            await ProcessFileUpload(id, "mseFile", "MSECertificate", oemFolder);
            await ProcessFileUpload(id, "startupFile", "StartupCertificate", oemFolder);
            await ProcessFileUpload(id, "localSupplierFile", "LocalSupplierCertificate", oemFolder);
            await ProcessFileUpload(id, "coRegFile", "CompanyRegistration", oemFolder);
            await ProcessFileUpload(id, "gstinFile", "GSTINCertificate", oemFolder);
            await ProcessFileUpload(id, "panFile", "PANCard", oemFolder);
            await ProcessFileUpload(id, "ctoFile", "CTOCertificate", oemFolder);

            await _context.SaveChangesAsync();
            return RedirectToAction("Step3", new { id });
        }
        #endregion

        #region Step 3: Key Personnel (Points 15, 16)
        [HttpGet]
        public async Task<IActionResult> Step3(int id)
        {
            var staff = await _context.StaffDetails.Where(s => s.ApplicationId == id).ToListAsync();
            ViewBag.AppId = id;
            ViewBag.Documents = await _context.ApplicationDocuments.Where(d => d.ApplicationId == id).ToListAsync();
            return View(staff);
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff(int id, StaffDetail staff)
        {
            var existingStaff = await _context.StaffDetails
                .FirstOrDefaultAsync(s => s.ApplicationId == id && s.StaffType == staff.StaffType);

            if (existingStaff != null)
            {
                existingStaff.Name = staff.Name;
                existingStaff.Designation = staff.Designation;
                existingStaff.MobileNo = staff.MobileNo;
                existingStaff.Email = staff.Email;
                existingStaff.Qualification = staff.Qualification ?? string.Empty;
            }
            else
            {
                var newStaff = new StaffDetail
                {
                    ApplicationId = id,
                    StaffType = staff.StaffType,
                    Name = staff.Name,
                    Designation = staff.Designation,
                    MobileNo = staff.MobileNo,
                    Email = staff.Email,
                    Qualification = staff.Qualification ?? string.Empty
                };
                _context.StaffDetails.Add(newStaff);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Step3", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SaveStep3Docs(int id)
        {
            string oemFolder = await GetOEMFolderName(id);
            await ProcessFileUpload(id, "orgChartFile", "OrganizationalChart", oemFolder);
            await ProcessFileUpload(id, "staffQualFile", "StaffQualification", oemFolder);
            var application = await _context.Applications.FindAsync(id);
            if (application != null) application.CurrentStep = Math.Max(application.CurrentStep, 4);
            await _context.SaveChangesAsync();
            return RedirectToAction("Step4", new { id });
        }
        #endregion

        #region Step 4: Technical Scope (Points 21, 22)
        [HttpGet]
        public async Task<IActionResult> Step4(int id)
        {
            var capabilities = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            var installations = await _context.InstallationRecords.Where(i => i.ApplicationId == id).ToListAsync();
            ViewBag.AppId = id;
            ViewBag.Documents = await _context.ApplicationDocuments.Where(d => d.ApplicationId == id).ToListAsync();
            ViewBag.Installations = installations;
            return View(capabilities);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCapabilities(int id, List<APCDCapability> capabilities, List<InstallationRecord> installations)
        {
            var existing = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            _context.APCDCapabilities.RemoveRange(existing);
            
            foreach(var cap in capabilities)
            {
                if (!string.IsNullOrWhiteSpace(cap.DesignedCapacity))
                {
                    cap.ApplicationId = id;
                    cap.MainType = cap.MainType ?? string.Empty;
                    cap.SubTech = cap.SubTech ?? string.Empty;
                    cap.Category = cap.Category ?? string.Empty;
                    cap.TypeDetails = cap.TypeDetails ?? string.Empty;
                    _context.APCDCapabilities.Add(cap);
                }
            }

            // Save installations
            if (installations != null && installations.Any())
            {
                var existingInstalls = await _context.InstallationRecords.Where(i => i.ApplicationId == id).ToListAsync();
                _context.InstallationRecords.RemoveRange(existingInstalls);

                string oemFolderInner = await GetOEMFolderName(id);

                int j = 0;
                foreach (var inst in installations)
                {
                    if (!string.IsNullOrWhiteSpace(inst.ClientName) || 
                        !string.IsNullOrWhiteSpace(inst.ApcdType) || 
                        inst.Year.HasValue)
                    {
                        var certFile = Request.Form.Files[$"PerformanceCertFile_{j}"];
                        if (certFile != null && certFile.Length > 0)
                        {
                            var path = await SaveFileAsync(certFile, oemFolderInner);
                            inst.PerformanceCertPath = path;
                        }

                        // Protect against NULL constraint crashes from empty bounds
                        inst.ClientName = inst.ClientName ?? string.Empty;
                        inst.ApcdType = inst.ApcdType ?? string.Empty;
                        inst.Capacity = inst.Capacity ?? string.Empty;
                        inst.PerformanceResult = inst.PerformanceResult ?? string.Empty;
                        inst.PerformanceCertPath = inst.PerformanceCertPath ?? string.Empty;
                        inst.Location = inst.Location ?? string.Empty;

                        inst.ApplicationId = id;
                        _context.InstallationRecords.Add(inst);
                    }
                    j++;
                }
            }
            
            string oemFolder = await GetOEMFolderName(id);
            
            await ProcessFileUpload(id, "productDatasheetFile", "ProductDatasheet", oemFolder);
            await ProcessFileUpload(id, "gaDrawingFile", "GADrawing", oemFolder);
            await ProcessFileUpload(id, "processFlowFile", "ProcessFlowDiagram", oemFolder);
            await ProcessFileUpload(id, "techCatalogueFile", "TechnicalCatalogue", oemFolder);
            await ProcessFileUpload(id, "designCalcFile", "DesignCalculation", oemFolder);
            await ProcessFileUpload(id, "materialConstructionFile", "MaterialOfConstruction", oemFolder);
            await ProcessFileUpload(id, "warrantyFile", "WarrantyDocument", oemFolder);
            await ProcessFileUpload(id, "installationExpFile", "InstallationExperience", oemFolder);

            var appModel = await _context.Applications.FindAsync(id);
            if (appModel != null) appModel.CurrentStep = Math.Max(appModel.CurrentStep, 5);

            await _context.SaveChangesAsync();
            return RedirectToAction("Step5", new { id });
        }
        #endregion

        #region Step 5: Financials & Documents (Points 17, 18, 23, 24)
        [HttpGet]
        public async Task<IActionResult> Step5(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Turnovers)
                .Include(a => a.Documents)
                .Include(a => a.Installations)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFinancials(int id, bool hasGrievance)
        {
            var application = await _context.Applications
                .Include(a => a.Turnovers)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (application == null || application.UserId != GetUserId()) return NotFound();

            application.HasGrievanceSystem = hasGrievance;

            var turnoverYears = new[] { "2022-23", "2023-24", "2024-25" };
            string oemFolder = await GetOEMFolderName(id);

            foreach (var year in turnoverYears)
            {
                var amountStr = Request.Form[$"TurnoverAmount_{year}"];
                if (decimal.TryParse(amountStr, out decimal amount))
                {
                    var turnover = application.Turnovers.FirstOrDefault(t => t.FinancialYear == year);
                    if (turnover == null)
                    {
                        turnover = new TurnoverRecord { ApplicationId = id, FinancialYear = year };
                        _context.TurnoverRecords.Add(turnover);
                        application.Turnovers.Add(turnover);
                    }
                    turnover.Amount = amount;
                }
            }

            // Save single consolidated turnover document
            await ProcessFileUpload(id, "consolidatedTurnoverFile", "ConsolidatedTurnover", oemFolder);

            await ProcessFileUpload(id, "bankSolvencyFile", "BankSolvency", oemFolder);
            await ProcessFileUpload(id, "bankAccountFile", "BankAccountDetails", oemFolder);
            await ProcessFileUpload(id, "serviceSupportFile", "ServiceSupportUndertaking", oemFolder);
            await ProcessFileUpload(id, "nonBlacklistingFile", "NonBlacklistingUndertaking", oemFolder);
            await ProcessFileUpload(id, "testCertificateFile", "TestCertificate", oemFolder);
            await ProcessFileUpload(id, "gstFilingFile", "GSTFiling", oemFolder);
            await ProcessFileUpload(id, "noLegalDisputesFile", "NoLegalDisputes", oemFolder);
            await ProcessFileUpload(id, "complaintPolicyFile", "ComplaintPolicy", oemFolder);
            await ProcessFileUpload(id, "escalationMechFile", "EscalationMechanism", oemFolder);
            await ProcessFileUpload(id, "unitPhotographsFile", "UnitPhotographs", oemFolder);

            for (int i = 1; i <= 3; i++)
            {
                await ProcessFileUpload(id, $"testimonialFile_{i}", $"ClientTestimonial_{i}", oemFolder);
            }

            application.CurrentStep = Math.Max(application.CurrentStep, 6);
            await _context.SaveChangesAsync();
            return RedirectToAction("Review", new { id });
        }

        private async Task<string> GetOEMFolderName(int applicationId)
        {
            var app = await _context.Applications
                .Include(a => a.User)
                .ThenInclude(u => u.CompanyProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            
            string companyName = app?.User?.CompanyProfile?.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName))
                companyName = $"OEM_{app?.UserId ?? 0}";
                
            var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Concat(new[] { ' ' }).ToArray();
            string safeName = new string(companyName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return safeName;
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{fileName}";
        }

        private async Task ProcessFileUpload(int id, string fileKey, string docType, string folderName)
        {
            var file = Request.Form.Files[fileKey];
            if (file != null && file.Length > 0)
            {
                var path = await SaveFileAsync(file, folderName);
                await AddOrUpdateDocument(id, docType, file.FileName, path);
            }
        }

        private async Task AddOrUpdateDocument(int applicationId, string documentType, string fileName, string filePath)
        {
            var doc = await _context.ApplicationDocuments
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.DocumentType == documentType);
            
            if (doc != null)
            {
                doc.FileName = fileName;
                doc.FilePath = filePath;
                doc.UploadedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ApplicationDocuments.Add(new ApplicationDocument
                {
                    ApplicationId = applicationId,
                    DocumentType = documentType,
                    FileName = fileName,
                    FilePath = filePath
                });
            }
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .ThenInclude(u => u.CompanyProfile)
                .Include(a => a.Documents)
                .Include(a => a.Installations)
                .Include(a => a.StaffDetails)
                .Include(a => a.Capabilities)
                .Include(a => a.Turnovers)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Capabilities)
                .Include(a => a.Payment)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            decimal baseAppFee = 25000;
            int apcdCount = application.Capabilities.Count(c => c.IsAppliedForEmpanelment);
            decimal baseEmpFee = apcdCount * 65000;

            decimal discountPercent = 0;
            if (application.IsMSE || application.IsStartup || application.IsLocalSupplier)
            {
                discountPercent = 0.15m;
            }

            // Application Fee Details
            decimal appFeeDiscount = baseAppFee * discountPercent;
            decimal appFeeNet = baseAppFee; // Full amount upfront
            decimal appFeeGST = appFeeNet * 0.18m;
            decimal appFeeTotal = appFeeNet + appFeeGST;

            // Empanelment Fee Details
            decimal empFeeDiscount = baseEmpFee * discountPercent;
            decimal empFeeNet = baseEmpFee; // Full amount upfront
            decimal empFeeGST = empFeeNet * 0.18m;
            decimal empFeeTotal = empFeeNet + empFeeGST;

            decimal total = appFeeTotal + empFeeTotal;

            ViewBag.BaseAppFee = baseAppFee;
            ViewBag.AppFeeDiscount = appFeeDiscount; // For reimbursement info
            ViewBag.AppFeeTotal = appFeeTotal;

            ViewBag.APCDCount = apcdCount;
            ViewBag.BaseEmpFee = baseEmpFee;
            ViewBag.EmpFeeDiscount = empFeeDiscount; // For reimbursement info
            ViewBag.EmpFeeTotal = empFeeTotal;

            var paymentDetail = application.Payment ?? new PaymentDetail { ApplicationId = id, Application = application };
            
            // Repopulate exact system calculations dynamically in case early form configurations changed
            paymentDetail.Amount = total;
            paymentDetail.APCDTypesCount = apcdCount;
            
            // Only initialize defaults if user hasn't overridden them with manual values yet
            if (paymentDetail.AppFeeAmountDeposited == 0) paymentDetail.AppFeeAmountDeposited = appFeeTotal;
            if (paymentDetail.EmpFeeAmountDeposited == 0) paymentDetail.EmpFeeAmountDeposited = empFeeTotal;

            return View(paymentDetail);
        }

        [HttpPost]
        public async Task<IActionResult> Payment(int id, PaymentDetail payment)
        {
            var appStatusGuard = await _context.Applications.FindAsync(id);
            if (appStatusGuard == null || appStatusGuard.Status != "Draft") return RedirectToAction("Review", new { id });
            
            payment.ApplicationId = id;
            payment.PaymentDate = DateTime.UtcNow;
            payment.Status = "Pending";

            // Fallback for legacy required fields if needed
            payment.UTRNumber = payment.AppFeeUTRNumber;
            payment.RemitterBank = payment.AppFeeRemitterBank;

            if (await _context.PaymentDetails.AnyAsync(p => p.ApplicationId == id))
            {
                var existing = await _context.PaymentDetails.FirstOrDefaultAsync(p => p.ApplicationId == id);
                _context.Entry(existing).CurrentValues.SetValues(payment);
            }
            else
            {
                _context.PaymentDetails.Add(payment);
            }

            string oemFolder = await GetOEMFolderName(id);
            await ProcessFileUpload(id, "paymentReceiptFile", "PaymentReceipt", oemFolder);

            var application = await _context.Applications.FindAsync(id);
            application.Status = "Submitted";
            application.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Submit", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Submit(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            return View(application);
        }
    }
}
