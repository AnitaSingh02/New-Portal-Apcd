using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APCD.Web.Models;
using APCD.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APCD.Web.Controllers
{
    [Authorize(Roles = "OEM,ADMIN,SUPER_ADMIN,OFFICER,COMMITTEE,FIELD_VERIFIER,DEALING_HAND")]
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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    application = new EmpanelmentApplication { UserId = userId, Status = "Draft", CurrentStep = 1 };
                    _context.Applications.Add(application);
                    await _context.SaveChangesAsync();

                    // Generate custom ApplicationId: NPC/APCD/APPL/{YEAR}/{COUNT}
                    string currentYear = DateTime.Now.Year.ToString();
                    string count = application.Id.ToString("D3");
                    application.ApplicationId = $"NPC/APCD/APPL/{currentYear}/{count}";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return RedirectToAction("Resume", new { id = application.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Resume(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return RedirectToAction("Index", "Dashboard");
            
            // If already submitted (ApplicationId exists or status isn't Draft), show the Success page
            if (!string.IsNullOrEmpty(application.ApplicationId) || application.Status != "Draft") 
            {
                return RedirectToAction("Submit", new { id });
            }

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
            
            if (profile == null)
            {
                var user = await _context.Users.FindAsync(userId);
                profile = new CompanyProfile 
                { 
                    UserId = userId,
                    CompanyName = user?.CompanyName ?? string.Empty,
                    GSTNumber = user?.GSTNumber ?? string.Empty 
                };
            }
            else if (string.IsNullOrEmpty(profile.GSTNumber) || string.IsNullOrEmpty(profile.CompanyName))
            {
                var user = await _context.Users.FindAsync(userId);
                if (string.IsNullOrEmpty(profile.GSTNumber)) profile.GSTNumber = user?.GSTNumber ?? string.Empty;
                if (string.IsNullOrEmpty(profile.CompanyName)) profile.CompanyName = user?.CompanyName ?? string.Empty;
            }

            ViewBag.AppId = id;
            return View(profile);
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
        public async Task<IActionResult> Step4(int id, string mode = null)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || (application.UserId != GetUserId() && !User.IsInRole("Admin"))) return RedirectToAction("Index", "Dashboard");

            var capabilities = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            var installations = await _context.InstallationRecords.Where(i => i.ApplicationId == id).ToListAsync();
            
            ViewBag.AppId = id;
            ViewBag.IsAddMoreMode = application.Status != "Draft" && application.Status != "Rejected";
            var allDocs = await _context.ApplicationDocuments.Where(d => d.ApplicationId == id).ToListAsync();
            ViewBag.Documents = allDocs;
            ViewBag.JsonDocuments = allDocs.Select(d => new { 
                d.DocumentType, 
                d.FileName, 
                d.FilePath, 
                d.AssociatedTech 
            }).ToList();
            
            ViewBag.Installations = installations;
            
            return View(capabilities);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCapabilities(int id, List<APCDCapability> capabilities, List<InstallationRecord> installations)
        {
            var application = await _context.Applications.Include(a => a.Capabilities).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            foreach (var cap in capabilities)
            {
                var existingCap = application.Capabilities.FirstOrDefault(c => c.MainType == cap.MainType && c.SubTech == cap.SubTech);
                if (existingCap != null)
                {
                    existingCap.IsManufactured = cap.IsManufactured;
                    existingCap.IsAppliedForEmpanelment = cap.IsAppliedForEmpanelment;
                    
                    if (!cap.IsManufactured && !cap.IsAppliedForEmpanelment)
                    {
                        existingCap.Category = string.Empty;
                        existingCap.DesignedCapacity = string.Empty;
                        existingCap.TypeDetails = string.Empty;

                        // Delete orphaned documents if the technology is completely deselected
                        var orphanedDocs = _context.ApplicationDocuments
                            .Where(d => d.ApplicationId == id && d.AssociatedTech == existingCap.SubTech)
                            .ToList();
                        if (orphanedDocs.Any())
                        {
                            _context.ApplicationDocuments.RemoveRange(orphanedDocs);
                        }
                    }
                    else
                    {
                        existingCap.Category = cap.Category ?? string.Empty;
                        existingCap.DesignedCapacity = cap.DesignedCapacity ?? string.Empty;
                        existingCap.TypeDetails = cap.TypeDetails ?? string.Empty;
                    }
                }
                else if (cap.IsManufactured || cap.IsAppliedForEmpanelment)
                {
                    cap.ApplicationId = id;
                    cap.MainType = cap.MainType ?? string.Empty;
                    cap.SubTech = cap.SubTech ?? string.Empty;
                    cap.Category = cap.Category ?? string.Empty;
                    cap.TypeDetails = cap.TypeDetails ?? string.Empty;
                    cap.DesignedCapacity = cap.DesignedCapacity ?? string.Empty;
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
            
            // Common documents (Card 13 stays common)
            await ProcessFileUpload(id, "techCatalogueFile", "TechnicalCatalogue", oemFolder);

            // Per-technology documents
            var appliedTechs = capabilities.Where(c => c.IsAppliedForEmpanelment).ToList();
            var docTypes = new Dictionary<string, string>
            {
                { "ProductDatasheet", "productDatasheetFile" },
                { "GADrawing", "gaDrawingFile" },
                { "ProcessFlowDiagram", "processFlowFile" },
                { "DesignCalculation", "designCalcFile" },
                { "MaterialOfConstruction", "materialConstructionFile" },
                { "WarrantyDocument", "warrantyFile" },
                { "InstallationExperience", "installationExpFile" },
                { "ClientPerformanceCertificate", "clientPerformanceFile" },
                { "TestCertificate", "testCertificateFile" } 
            };

            foreach (var tech in appliedTechs)
            {
                string safeTechName = tech.SubTech.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                foreach (var docType in docTypes)
                {
                    string fileKey = $"{docType.Value}_{safeTechName}";
                    await ProcessFileUpload(id, fileKey, docType.Key, oemFolder, tech.SubTech);
                }
            }

            // Update SelectedAPCDCategories summary for fee calculation and review
            application.SelectedAPCDCategories = string.Join(",", application.Capabilities
                .Where(c => c.IsAppliedForEmpanelment)
                .Select(c => c.SubTech));

            if (application != null) application.CurrentStep = Math.Max(application.CurrentStep, 5);

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
            
            if (application == null) return NotFound();

            // Calculate dynamic financial years (Last 3 COMPLETED years)
            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;
            // FY starts in April. If today is April 2026 or later, 2025-26 just finished.
            int lastCompletedYear = (currentMonth >= 4) ? (currentYear - 1) : (currentYear - 2);
            
            var years = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                int y = lastCompletedYear - i;
                years.Add($"{y}-{(y + 1) % 100:D2}");
            }
            ViewBag.FinancialYears = years;
            
            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFinancials(int id, bool hasGrievance)
        {
            var application = await _context.Applications
                .Include(a => a.Turnovers)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (application == null || (application.UserId != GetUserId() && !User.IsInRole("Admin"))) return NotFound();

            application.HasGrievanceSystem = hasGrievance;

            // Recalculate dynamic years to match the form fields sent by the dynamic View
            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;
            int lastCompletedYear = (currentMonth >= 4) ? (currentYear - 1) : (currentYear - 2);
            var turnoverYears = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                int y = lastCompletedYear - i;
                turnoverYears.Add($"{y}-{(y + 1) % 100:D2}");
            }

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

            // Save mandatory documents
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

        private async Task ProcessFileUpload(int id, string fileKey, string docType, string folderName, string associatedTech = "")
        {
            var file = Request.Form.Files[fileKey];
            if (file != null && file.Length > 0)
            {
                var path = await SaveFileAsync(file, folderName);
                await AddOrUpdateDocument(id, docType, file.FileName, path, associatedTech);
            }
        }

        private async Task AddOrUpdateDocument(int applicationId, string documentType, string fileName, string filePath, string associatedTech = "")
        {
            var query = _context.ApplicationDocuments
                .Where(d => d.ApplicationId == applicationId && d.DocumentType == documentType);

            if (!string.IsNullOrEmpty(associatedTech))
            {
                query = query.Where(d => d.AssociatedTech == associatedTech);
            }

            var doc = await query.FirstOrDefaultAsync();
            
            if (doc != null)
            {
                doc.FileName = fileName;
                doc.FilePath = filePath;
                doc.UploadedAt = DateTime.UtcNow;
                doc.AssociatedTech = associatedTech;
            }
            else
            {
                _context.ApplicationDocuments.Add(new ApplicationDocument
                {
                    ApplicationId = applicationId,
                    DocumentType = documentType,
                    FileName = fileName,
                    FilePath = filePath,
                    AssociatedTech = associatedTech
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
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Capabilities)
                .Include(a => a.Payments)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            // Calculate Fees
            decimal baseAppFee = 25000;
            int currentApcdCount = application.Capabilities.Count(c => c.IsAppliedForEmpanelment);
            decimal baseEmpFee = currentApcdCount * 65000;

            decimal discountPercent = 0;
            if (application.IsMSE || application.IsStartup || application.IsLocalSupplier)
            {
                discountPercent = 0.15m;
            }

            decimal appFeeTotal = (baseAppFee) * 1.18m;
            decimal empFeeTotal = (baseEmpFee) * 1.18m;
            decimal total = appFeeTotal + empFeeTotal;

            ViewBag.APCDCount = currentApcdCount;
            ViewBag.EmpFeeTotal = empFeeTotal;
            ViewBag.AppFeeTotal = appFeeTotal;
            ViewBag.DiscountPercent = (int)(discountPercent * 100);

            // Fetch the primary EmpFee record to show its specific count in history
            var initialEmpFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.EmpFee);
            
            // BACKWARD COMPATIBILITY FIX: 
            // If we have an EmpFee payment but devices aren't marked IsPaid, mark them now.
            // This ensures "Previously Paid" doesn't show 0 for existing applications.
            if (initialEmpFee != null && !application.Capabilities.Any(c => c.IsPaid))
            {
                var paidDevices = application.Capabilities.Where(c => c.IsAppliedForEmpanelment).Take(initialEmpFee.APCDTypesCount ?? 0).ToList();
                foreach(var d in paidDevices) { d.IsPaid = true; d.PaymentId = initialEmpFee.Id; }
                await _context.SaveChangesAsync();
            }

            var paymentDetail = new PaymentViewModel { ApplicationId = id, Application = application };
            var appFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.AppFee);
            var empFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.EmpFee);
            var suppFeeList = application.Payments.Where(p => p.Type == PaymentType.Supplemental).ToList();

            if (appFee != null)
            {
                paymentDetail.AppFeeAmountDeposited = (decimal)appFee.Amount;
                paymentDetail.AppFeeRemitterBank = appFee.RemitterBank;
                paymentDetail.AppFeeUTRNumber = appFee.UTRNumber;
                paymentDetail.AppFeePaymentDate = appFee.PaymentDate;
            }
            if (empFee != null)
            {
                paymentDetail.EmpFeeAmountDeposited = (decimal)empFee.Amount;
                paymentDetail.EmpFeeRemitterBank = empFee.RemitterBank;
                paymentDetail.EmpFeeUTRNumber = empFee.UTRNumber;
                paymentDetail.EmpFeePaymentDate = empFee.PaymentDate;
                paymentDetail.APCDTypesCount = empFee.APCDTypesCount ?? 0;
            }
            // Supplemental payments are handled as a new entry below
            paymentDetail.SupplementalAmount = null;
            paymentDetail.SupplementalUTR = string.Empty;
            paymentDetail.SupplementalPayDate = null;
            
            // --- Supplemental Payment detection ---
            if (application.Status != "Draft")
            {
                var unpaidDevices = application.Capabilities.Where(c => c.IsAppliedForEmpanelment && !c.IsPaid).ToList();
                int extraUnits = unpaidDevices.Count;
                
                if (extraUnits > 0)
                {
                    decimal extraBase = extraUnits * 65000;
                    decimal extraGST = extraBase * 0.18m;
                    ViewBag.BalanceDue = extraBase + extraGST;
                    ViewBag.IsSupplemental = true;
                    ViewBag.UnpaidCount = extraUnits;
                    ViewBag.PaidCount = application.Capabilities.Count(c => c.IsAppliedForEmpanelment && c.IsPaid);
                    ViewBag.TotalCount = currentApcdCount;

                    // AUTO-CALCULATE amount for the NEW supplemental payment
                    paymentDetail.SupplementalAmount = extraBase + extraGST;
                    paymentDetail.SupplementalUTR = string.Empty;
                    paymentDetail.SupplementalPayDate = null;
                }
                else
                {
                    ViewBag.BalanceDue = 0;
                    ViewBag.IsSupplemental = false;
                }
            }

            // Initial calculation for Draft
            if (application.Status == "Draft")
            {
                paymentDetail.Amount = total;
                paymentDetail.APCDTypesCount = currentApcdCount;
                if (paymentDetail.AppFeeAmountDeposited == 0) paymentDetail.AppFeeAmountDeposited = appFeeTotal;
                if (paymentDetail.EmpFeeAmountDeposited == 0) paymentDetail.EmpFeeAmountDeposited = empFeeTotal;
            }

            return View(paymentDetail);
        }

        public async Task<IActionResult> Payment(int id, PaymentViewModel payment)
        {
            var application = await _context.Applications.Include(a => a.Payments).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            if (application.Status == "Draft")
            {
                var appFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.AppFee);
                if (appFee == null) { appFee = new Payment { ApplicationId = id, Type = PaymentType.AppFee }; _context.Payments.Add(appFee); }
                appFee.Amount = payment.AppFeeAmountDeposited;
                appFee.UTRNumber = payment.AppFeeUTRNumber;
                appFee.RemitterBank = payment.AppFeeRemitterBank;
                appFee.PaymentDate = payment.AppFeePaymentDate ?? DateTime.UtcNow;

                var empFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.EmpFee);
                if (empFee == null) { empFee = new Payment { ApplicationId = id, Type = PaymentType.EmpFee }; _context.Payments.Add(empFee); }
                empFee.Amount = payment.EmpFeeAmountDeposited;
                empFee.UTRNumber = payment.EmpFeeUTRNumber;
                empFee.RemitterBank = payment.EmpFeeRemitterBank;
                empFee.PaymentDate = payment.EmpFeePaymentDate ?? DateTime.UtcNow;
                
                await _context.SaveChangesAsync(); // Save to get empFee.Id

                // Mark devices as paid
                var devices = await _context.APCDCapabilities
                    .Where(c => c.ApplicationId == id && c.IsAppliedForEmpanelment)
                    .ToListAsync();
                foreach(var dev in devices) {
                    dev.IsPaid = true;
                    dev.PaymentId = empFee.Id;
                }
                empFee.APCDTypesCount = devices.Count;

                application.Status = "Submitted";
                application.SubmittedAt = DateTime.UtcNow;
            }
            else
            {
                // Supplemental Payment logic
                if (payment.SupplementalAmount > 0 && !string.IsNullOrEmpty(payment.SupplementalUTR))
                {
                    var suppFee = new Payment 
                    { 
                        ApplicationId = id, 
                        Type = PaymentType.Supplemental,
                        IsSupplemental = true,
                        Amount = payment.SupplementalAmount.Value,
                        UTRNumber = payment.SupplementalUTR,
                        RemitterBank = "Unknown",
                        PaymentDate = payment.SupplementalPayDate ?? DateTime.UtcNow,
                        Status = "Pending"
                    };
                    _context.Payments.Add(suppFee);
                    await _context.SaveChangesAsync();

                    // Link only newly added (unpaid) devices
                    var unpaidDevices = await _context.APCDCapabilities
                        .Where(c => c.ApplicationId == id && c.IsAppliedForEmpanelment && !c.IsPaid)
                        .ToListAsync();

                    foreach (var dev in unpaidDevices)
                    {
                        dev.IsPaid = true;
                        dev.PaymentId = suppFee.Id;
                    }
                    suppFee.APCDTypesCount = unpaidDevices.Count;
                }
            }

            string oemFolder = await GetOEMFolderName(id);
            await ProcessFileUpload(id, "paymentReceiptFile", "PaymentReceipt", oemFolder);
            await ProcessFileUpload(id, "supplementalReceiptFile", "SupplementalReceipt", oemFolder);

            await _context.SaveChangesAsync();
            return RedirectToAction("Submit", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAmendment(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            // When adding more tech, we move from 'Submitted/Approved' back to a state that needs review
            // For now, moving back to 'Submitted' is sufficient to appear on Admin Dashboard
            application.Status = "Submitted";
            application.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Submit", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Submit(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();
            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var userId = GetUserId();
            var isInternal = User.IsInRole("ADMIN") || User.IsInRole("SUPER_ADMIN") || User.IsInRole("OFFICER") || 
                             User.IsInRole("COMMITTEE") || User.IsInRole("FIELD_VERIFIER") || User.IsInRole("DEALING_HAND");

            var application = await _context.Applications
                .Include(a => a.User)
                .ThenInclude(u => u.CompanyProfile)
                .Include(a => a.Documents)
                .Include(a => a.Installations)
                .Include(a => a.StaffDetails)
                .Include(a => a.Capabilities)
                .Include(a => a.Turnovers)
                .Include(a => a.Payments)
                .Include(a => a.Remarks)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            // Ownership check: Only the owner OEM or any Internal role can view
            if (!isInternal && application.UserId != userId)
            {
                return Forbid();
            }

            return View(application);
        }
    }
}
