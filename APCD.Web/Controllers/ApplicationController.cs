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
        public async Task<IActionResult> AddMoreAPCD(int id)
        {
            var userId = GetUserId();
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != userId) return NotFound();

            // Check if there's already a Draft SupplementalRequest
            var existingDraft = await _context.SupplementalRequests
                .FirstOrDefaultAsync(r => r.ApplicationId == id && r.Status == "Draft");

            if (existingDraft == null)
            {
                existingDraft = new SupplementalRequest
                {
                    ApplicationId = id,
                    UserId = userId,
                    Status = "Draft",
                    LastCompletedStep = 4
                };
                _context.SupplementalRequests.Add(existingDraft);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Step4", new { id, supplementalId = existingDraft.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Resume(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return RedirectToAction("Index", "Dashboard");
            
            // If already submitted, we can still resume but maybe start at Step 1 for review
            // or just follow the CurrentStep if they were in the middle of a post-submission view
            if (application.Status != "Draft") 
            {
                return RedirectToAction("Step1", new { id });
            }

            return application.CurrentStep switch
            {
                2 => RedirectToAction("Step2", new { id }),
                3 => RedirectToAction("Step3", new { id }),
                4 => RedirectToAction("Step4", new { id }),
                5 => RedirectToAction("Step5", new { id }),
                6 => RedirectToAction("Review", new { id }),
                7 => RedirectToAction("Payment", new { id }),
                _ => RedirectToAction("Step1", new { id })
            };
        }

        #region Step 1: Company Profile (Points 1-6, 9-13)
        [HttpGet]
        public async Task<IActionResult> Step1(int id)
        {
            var userId = GetUserId();
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != userId) return NotFound();

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
            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;
            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Step1(int id, CompanyProfile profile)
        {
            var userId = GetUserId();
            var app = await _context.Applications.FindAsync(id);
            if (app == null || app.UserId != userId) return NotFound();

            // Guard: Prevent changes if already submitted
            if (app.Status != "Draft") return RedirectToAction("Step2", new { id });

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
            
            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;
            ViewBag.JsonDocuments = application.Documents.Select(d => new { d.DocumentType, d.AssociatedTech, d.FileName, d.FilePath }).ToList();
            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> Step2(int id, EmpanelmentApplication model)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            // Guard: Prevent changes if already submitted
            if (application.Status != "Draft") return RedirectToAction("Step3", new { id });

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
            string category = "Common";
            int step = 2;

            await ProcessFileUpload(id, "isoStandardsFile", "ISOStandardsCertificate", oemFolder, step, category);
            await ProcessFileUpload(id, "mseFile", "MSECertificate", oemFolder, step, category);
            await ProcessFileUpload(id, "startupFile", "StartupCertificate", oemFolder, step, category);
            await ProcessFileUpload(id, "localSupplierFile", "LocalSupplierCertificate", oemFolder, step, category);
            await ProcessFileUpload(id, "coRegFile", "CompanyRegistration", oemFolder, step, category);
            await ProcessFileUpload(id, "gstinFile", "GSTINCertificate", oemFolder, step, category);
            await ProcessFileUpload(id, "panFile", "PANCard", oemFolder, step, category);
            await ProcessFileUpload(id, "ctoFile", "CTOCertificate", oemFolder, step, category);

            await _context.SaveChangesAsync();
            return RedirectToAction("Step3", new { id });
        }
        #endregion

        #region Step 3: Key Personnel (Points 15, 16)
        [HttpGet]
        public async Task<IActionResult> Step3(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            var staff = await _context.StaffDetails.Where(s => s.ApplicationId == id).ToListAsync();
            ViewBag.AppId = id;
            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;
            var allDocs = await _context.ApplicationDocuments.Where(d => d.ApplicationId == id).ToListAsync();
            ViewBag.Documents = allDocs;
            ViewBag.JsonDocuments = allDocs.Select(d => new { d.DocumentType, d.AssociatedTech, d.FileName, d.FilePath }).ToList();
            return View(staff);
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff(int id, StaffDetail staff)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            // Guard: Prevent changes if already submitted
            if (application.Status != "Draft") return RedirectToAction("Step3", new { id });

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
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            // Guard: Prevent changes if already submitted
            if (application.Status != "Draft") return RedirectToAction("Step4", new { id });

            string oemFolder = await GetOEMFolderName(id);
            await ProcessFileUpload(id, "orgChartFile", "OrganizationalChart", oemFolder, 3, "Common");
            await ProcessFileUpload(id, "staffQualFile", "StaffQualification", oemFolder, 3, "Common");
            application.CurrentStep = Math.Max(application.CurrentStep, 4);
            await _context.SaveChangesAsync();
            return RedirectToAction("Step4", new { id });
        }
        #endregion

        #region Step 4: Technical Scope (Points 21, 22)
        [HttpGet]
        public async Task<IActionResult> Step4(int id, int? supplementalId = null)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || (application.UserId != GetUserId() && !User.IsInRole("Admin"))) return RedirectToAction("Index", "Dashboard");

            if (application.Status == "Draft" && application.CurrentStep < 4)
            {
                application.CurrentStep = 4;
                await _context.SaveChangesAsync();
            }

            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;
            ViewBag.SupplementalId = supplementalId;
            ViewBag.IsSupplementalMode = supplementalId.HasValue;

            var capabilities = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            
            if (supplementalId.HasValue)
            {
                var supRequest = await _context.SupplementalRequests
                    .Include(r => r.Devices)
                    .Include(r => r.Documents)
                    .FirstOrDefaultAsync(r => r.Id == supplementalId && r.ApplicationId == id);
                
                if (supRequest == null) return NotFound();
                
                // Map SupplementalDevices back to capabilities for the view to show drafts
                foreach(var supDev in supRequest.Devices)
                {
                    var existing = capabilities.FirstOrDefault(c => c.MainType == supDev.MainType && c.SubTech == supDev.SubTech);
                    if (existing != null)
                    {
                        existing.IsAppliedForEmpanelment = true;
                        existing.Category = supDev.Category;
                        existing.DesignedCapacity = supDev.DesignedCapacity;
                    }
                    else
                    {
                        capabilities.Add(new APCDCapability {
                            MainType = supDev.MainType,
                            SubTech = supDev.SubTech,
                            IsAppliedForEmpanelment = true,
                            Category = supDev.Category,
                            DesignedCapacity = supDev.DesignedCapacity
                        });
                    }
                }
                ViewBag.SupplementalDocuments = supRequest.Documents.ToList();
            }

            var installations = await _context.InstallationRecords.Where(i => i.ApplicationId == id).ToListAsync();
            
            ViewBag.AppId = id;
            ViewBag.IsAddMoreMode = supplementalId.HasValue;
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
        public async Task<IActionResult> SaveCapabilities(int id, List<APCDCapability> capabilities, List<InstallationRecord> installations, int? supplementalId = null)
        {
            var application = await _context.Applications.Include(a => a.Capabilities).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            if (supplementalId.HasValue)
            {
                var supRequest = await _context.SupplementalRequests
                    .Include(r => r.Devices)
                    .Include(r => r.Documents)
                    .FirstOrDefaultAsync(r => r.Id == supplementalId && r.ApplicationId == id);

                if (supRequest == null) return NotFound();

                // Save selections to SupplementalDevices (Draft)
                _context.SupplementalDevices.RemoveRange(supRequest.Devices);
                var newDevices = capabilities.Where(c => c.IsAppliedForEmpanelment).ToList();
                
                // Don't allow re-submitting already paid devices in supplemental flow
                var alreadyPaid = application.Capabilities.Where(c => c.IsPaid).Select(c => c.SubTech).ToList();
                
                foreach (var cap in newDevices)
                {
                    if (alreadyPaid.Contains(cap.SubTech)) continue;

                    var supDev = new SupplementalDevice
                    {
                        SupplementalRequestId = supplementalId.Value,
                        MainType = cap.MainType ?? string.Empty,
                        SubTech = cap.SubTech ?? string.Empty,
                        Category = cap.Category ?? string.Empty,
                        DesignedCapacity = cap.DesignedCapacity ?? string.Empty,
                        Status = "Draft"
                    };
                    _context.SupplementalDevices.Add(supDev);

                    // Process files for this new device
                    var supDocTypes = new[] { "ProductDatasheet", "GADrawing", "ProcessFlowDiagram", "DesignCalculation", 
                                          "MaterialOfConstruction", "WarrantyDocument", "InstallationExperience", 
                                          "ClientPerformanceCertificate", "TestCertificate" };
                    
                    var supFormTypes = new Dictionary<string, string>
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

                    string safeTechName = cap.SubTech.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                    foreach (var dt in supDocTypes)
                    {
                        string fileKey = $"{supFormTypes[dt]}_{safeTechName}";
                        await ProcessSupplementalFileUpload(id, supplementalId.Value, fileKey, dt, cap.SubTech);
                    }
                }

                supRequest.LastCompletedStep = 4;
                supRequest.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return RedirectToAction("Payment", new { id, supplementalId });
            }

            bool isAddMoreMode = supplementalId.HasValue;

            // Guard: Prevent changes if already submitted (except in Add More mode)
            if (application.Status != "Draft" && !isAddMoreMode) return RedirectToAction("Step5", new { id });

            foreach (var cap in capabilities)
            {
                var existingCap = application.Capabilities.FirstOrDefault(c => c.MainType == cap.MainType && c.SubTech == cap.SubTech);
                
                // If in Add More mode, DO NOT touch previously applied technologies
                if (isAddMoreMode && existingCap != null && existingCap.IsAppliedForEmpanelment)
                {
                    continue;
                }

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

            // Save installations - Skip if in Add More mode
            if (!isAddMoreMode && installations != null && installations.Any())
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
            
            // Common documents (Card 13 stays common) - Skip if in Add More mode
            if (!isAddMoreMode)
            {
                await ProcessFileUpload(id, "techCatalogueFile", "TechnicalCatalogue", oemFolder, 4, "Common");
            }

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
                // In Add More mode, ONLY process documents for NEWLY applied technologies
                if (isAddMoreMode)
                {
                    var wasAlreadyApplied = application.Capabilities.Any(c => c.SubTech == tech.SubTech && c.IsAppliedForEmpanelment);
                    if (wasAlreadyApplied) continue;
                }

                string safeTechName = tech.SubTech.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                foreach (var docType in docTypes)
                {
                    string fileKey = $"{docType.Value}_{safeTechName}";
                    await ProcessFileUpload(id, fileKey, docType.Key, oemFolder, 4, "APCD", tech.SubTech);
                }
            }

            // Update SelectedAPCDCategories summary for fee calculation and review
            application.SelectedAPCDCategories = string.Join(",", application.Capabilities
                .Where(c => c.IsAppliedForEmpanelment)
                .Select(c => c.SubTech));

            if (application.Status == "Draft")
            {
                application.CurrentStep = Math.Max(application.CurrentStep, 5);
            }

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
            
            if (application == null || application.UserId != GetUserId()) return NotFound();

            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;

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
            ViewBag.JsonDocuments = application.Documents.Select(d => new { d.DocumentType, d.AssociatedTech, d.FileName, d.FilePath }).ToList();
            
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

            // Guard: Prevent changes if already submitted
            if (application.Status != "Draft") return RedirectToAction("Review", new { id });

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
            await ProcessFileUpload(id, "consolidatedTurnoverFile", "ConsolidatedTurnover", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "bankSolvencyFile", "BankSolvency", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "bankAccountFile", "BankAccountDetails", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "serviceSupportFile", "ServiceSupportUndertaking", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "nonBlacklistingFile", "NonBlacklistingUndertaking", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "testCertificateFile", "TestCertificate", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "gstFilingFile", "GSTFiling", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "noLegalDisputesFile", "NoLegalDisputes", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "complaintPolicyFile", "ComplaintPolicy", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "escalationMechFile", "EscalationMechanism", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "unitPhotographsFile", "UnitPhotographs", oemFolder, 5, "Common");

            for (int i = 1; i <= 3; i++)
            {
                await ProcessFileUpload(id, $"testimonialFile_{i}", $"ClientTestimonial_{i}", oemFolder, 5, "Common");
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

        private async Task ProcessFileUpload(int id, string fileKey, string docType, string folderName, int step, string category, string associatedTech = "")
        {
            var file = Request.Form.Files[fileKey];
            if (file != null && file.Length > 0)
            {
                // Strict path requirement: /wwwroot/uploads/applications/{ApplicationId}/initial/{APCDType}/{DocumentType}/
                string safeTechName = string.IsNullOrEmpty(associatedTech) ? "Common" : associatedTech.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                string safeDocType = docType.Replace(" ", "_");
                
                string subPath = $"applications/{id}/initial/{safeTechName}/{safeDocType}";
                string fullPath = Path.Combine(_environment.WebRootPath, "uploads", subPath);
                
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(fullPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string webPath = $"/uploads/{subPath}/{fileName}";
                
                // For Installation certificates in Step 4, we use a different table
                if (docType == "PerformanceCertificate" && associatedTech.StartsWith("Installation_"))
                {
                    int index = int.Parse(associatedTech.Split('_')[1]);
                    var installations = await _context.InstallationRecords
                        .Where(i => i.ApplicationId == id)
                        .OrderBy(i => i.Id)
                        .ToListAsync();

                    if (index < installations.Count)
                    {
                        installations[index].PerformanceCertPath = webPath;
                    }
                    else
                    {
                        var newInst = new InstallationRecord { ApplicationId = id, PerformanceCertPath = webPath };
                        _context.InstallationRecords.Add(newInst);
                    }
                }
                else
                {
                    var query = _context.ApplicationDocuments.Where(d => d.ApplicationId == id && d.DocumentType == docType && d.AssociatedTech == (associatedTech ?? ""));
                    
                    var doc = await query.FirstOrDefaultAsync();
                    if (doc == null)
                    {
                        doc = new ApplicationDocument
                        {
                            ApplicationId = id,
                            DocumentType = docType,
                            AssociatedTech = associatedTech,
                            DocumentCategory = string.IsNullOrEmpty(associatedTech) ? "Common" : "APCD",
                            StepNumber = step
                        };
                        _context.ApplicationDocuments.Add(doc);
                    }

                    doc.FileName = file.FileName;
                    doc.FilePath = webPath;
                    doc.UploadedAt = DateTime.UtcNow;
                    doc.IsActive = true;
                }
            }
        }

        private async Task ProcessSupplementalFileUpload(int applicationId, int supplementalId, string fileKey, string docType, string apcdType)
        {
            var file = Request.Form.Files[fileKey];
            if (file != null && file.Length > 0)
            {
                // Custom path: /uploads/applications/{ApplicationId}/supplemental/{SupplementalRequestId}/{APCDType}/{DocumentType}/
                string safeTechName = apcdType.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                string subPath = $"applications/{applicationId}/supplemental/{supplementalId}/{safeTechName}/{docType}";
                var path = await SaveFileAsync(file, subPath);
                
                var supDoc = await _context.SupplementalDocuments
                    .FirstOrDefaultAsync(d => d.SupplementalRequestId == supplementalId && d.DocumentType == docType && d.APCDType == apcdType);
                
                if (supDoc == null)
                {
                    supDoc = new SupplementalDocument
                    {
                        SupplementalRequestId = supplementalId,
                        APCDType = apcdType,
                        DocumentType = docType
                    };
                    _context.SupplementalDocuments.Add(supDoc);
                }
                
                supDoc.FileName = file.FileName;
                supDoc.FilePath = path;
                supDoc.Status = "Draft";
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(int applicationId, string documentType, string associatedTech, int stepNumber, IFormFile file)
        {
            var userId = GetUserId();
            var application = await _context.Applications.FindAsync(applicationId);
            if (application == null || application.UserId != userId) return Json(new { success = false, message = "Application not found or unauthorized." });

            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });

            try
            {
                // Strict path requirement: /wwwroot/uploads/applications/{ApplicationId}/initial/{APCDType}/{DocumentType}/
                string safeTechName = string.IsNullOrEmpty(associatedTech) ? "Common" : associatedTech.Replace(" ", "_").Replace("(", "").Replace(")", "").Replace("/", "_");
                string safeDocType = documentType.Replace(" ", "_");
                
                string subPath = $"applications/{applicationId}/initial/{safeTechName}/{safeDocType}";
                string fullPath = Path.Combine(_environment.WebRootPath, "uploads", subPath);
                
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(fullPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string webPath = $"/uploads/{subPath}/{fileName}";

                if (documentType == "PerformanceCertificate" && associatedTech.StartsWith("Installation_"))
                {
                    int index = int.Parse(associatedTech.Split('_')[1]);
                    var installations = await _context.InstallationRecords
                        .Where(i => i.ApplicationId == applicationId)
                        .OrderBy(i => i.Id)
                        .ToListAsync();

                    // If we have enough installations, update the existing one. 
                    // If not, we might need to create placeholders or wait for SaveCapabilities.
                    // But for autosave, we should at least try to update if it exists.
                    if (index < installations.Count)
                    {
                        installations[index].PerformanceCertPath = webPath;
                    }
                    else
                    {
                        // Create a placeholder installation if it doesn't exist yet to hold the path
                        var newInst = new InstallationRecord { ApplicationId = applicationId, PerformanceCertPath = webPath };
                        _context.InstallationRecords.Add(newInst);
                    }
                }
                else if (documentType == "SupplementalPaymentReceipt")
                {
                    // associatedTech will contain the SupplementalRequestId as "Supplemental_ID"
                    if (string.IsNullOrEmpty(associatedTech) || !associatedTech.StartsWith("Supplemental_"))
                        return Json(new { success = false, message = "Invalid supplemental ID." });

                    int supId = int.Parse(associatedTech.Split('_')[1]);
                    var supPay = await _context.SupplementalPayments
                        .FirstOrDefaultAsync(p => p.SupplementalRequestId == supId);

                    if (supPay == null)
                    {
                        supPay = new SupplementalPayment { SupplementalRequestId = supId };
                        _context.SupplementalPayments.Add(supPay);
                    }
                    supPay.ReceiptPath = webPath;
                    supPay.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Update or Create ApplicationDocument
                    var query = _context.ApplicationDocuments.Where(d => d.ApplicationId == applicationId && d.DocumentType == documentType && d.AssociatedTech == (associatedTech ?? ""));
                    
                    var doc = await query.FirstOrDefaultAsync();
                    if (doc == null)
                    {
                        doc = new ApplicationDocument
                        {
                            ApplicationId = applicationId,
                            DocumentType = documentType,
                            AssociatedTech = associatedTech ?? "",
                            DocumentCategory = string.IsNullOrEmpty(associatedTech) ? "Common" : "APCD",
                            StepNumber = stepNumber > 0 ? stepNumber : 4
                        };
                        _context.ApplicationDocuments.Add(doc);
                    }

                    doc.FileName = file.FileName;
                    doc.FilePath = webPath;
                    doc.UploadedAt = DateTime.UtcNow;
                    doc.IsActive = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, fileName = file.FileName, filePath = webPath });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " -> " + ex.InnerException.Message;
                return Json(new { success = false, message = msg });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int applicationId, string documentType, string associatedTech)
        {
            var userId = GetUserId();
            var application = await _context.Applications.FindAsync(applicationId);
            if (application == null || application.UserId != userId) return Json(new { success = false, message = "Application not found or unauthorized." });

            if (documentType == "PerformanceCertificate" && associatedTech.StartsWith("Installation_"))
            {
                int index = int.Parse(associatedTech.Split('_')[1]);
                var installations = await _context.InstallationRecords
                    .Where(i => i.ApplicationId == applicationId)
                    .OrderBy(i => i.Id)
                    .ToListAsync();

                if (index < installations.Count)
                {
                    installations[index].PerformanceCertPath = string.Empty;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }
            else if (documentType == "SupplementalPaymentReceipt")
            {
                if (string.IsNullOrEmpty(associatedTech) || !associatedTech.StartsWith("Supplemental_"))
                    return Json(new { success = false, message = "Invalid supplemental ID." });

                int supId = int.Parse(associatedTech.Split('_')[1]);
                var supPay = await _context.SupplementalPayments
                    .FirstOrDefaultAsync(p => p.SupplementalRequestId == supId);

                if (supPay != null)
                {
                    supPay.ReceiptPath = string.Empty;
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true });
            }
            else
            {
                var query = _context.ApplicationDocuments.Where(d => d.ApplicationId == applicationId && d.DocumentType == documentType && d.AssociatedTech == (associatedTech ?? ""));
                
                var doc = await query.FirstOrDefaultAsync();
                if (doc != null)
                {
                    _context.ApplicationDocuments.Remove(doc);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, message = "Document not found." });
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

            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id, int? supplementalId = null)
        {
            var application = await _context.Applications
                .Include(a => a.Capabilities)
                .Include(a => a.Payments)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = application.Status == "Draft" ? application.CurrentStep : 8;
            ViewBag.SupplementalId = supplementalId;

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

            var paymentDetail = new PaymentViewModel { ApplicationId = id, Application = application };

            if (supplementalId.HasValue)
            {
                var supRequest = await _context.SupplementalRequests
                    .Include(r => r.Devices)
                    .Include(r => r.Payments)
                    .Include(r => r.Documents)
                    .FirstOrDefaultAsync(r => r.Id == supplementalId && r.ApplicationId == id);
                
                if (supRequest == null) return NotFound();

                int extraUnits = supRequest.Devices.Count;
                decimal extraBase = extraUnits * 65000;
                decimal extraGST = extraBase * 0.18m;

                ViewBag.BalanceDue = extraBase + extraGST;
                ViewBag.IsSupplemental = true;
                ViewBag.UnpaidCount = extraUnits;
                ViewBag.PaidCount = application.Capabilities.Count(c => c.IsAppliedForEmpanelment && c.IsPaid);
                ViewBag.TotalCount = currentApcdCount + extraUnits;

                paymentDetail.SupplementalAmount = extraBase + extraGST;
                paymentDetail.APCDTypesCount = extraUnits;
                ViewBag.NewApcdTypes = string.Join(", ", supRequest.Devices.Select(d => d.SubTech));
                ViewBag.NewApcdCount = extraUnits;
                ViewBag.ExtraBase = extraBase;
                ViewBag.ExtraGST = extraGST;
                
                var existingSupPay = supRequest.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
                if (existingSupPay != null)
                {
                    paymentDetail.SupplementalUTR = existingSupPay.UTRNumber;
                    paymentDetail.SupplementalPayDate = existingSupPay.PaymentDate;
                    paymentDetail.SupplementalBankName = existingSupPay.BankName;
                    paymentDetail.SupplementalAmountDeposited = existingSupPay.AmountDeposited;
                    paymentDetail.SupplementalReceiptPath = existingSupPay.ReceiptPath;
                    ViewBag.SupplementalPaymentSaved = !string.IsNullOrEmpty(existingSupPay.UTRNumber);
                }
            }
            else
            {
                // BACKWARD COMPATIBILITY FIX: 
                // Ensure IsPaid flags match the total units paid in transaction history
                int totalUnitsPaidInHistory = application.Payments
                    .Where(p => (p.Type == PaymentType.EmpFee || p.Type == PaymentType.Supplemental) && p.Status != "Rejected")
                    .ToList()
                    .Sum(p => p.APCDTypesCount.HasValue && p.APCDTypesCount.Value > 0 
                        ? p.APCDTypesCount.Value 
                        : (p.Amount.HasValue ? (int)Math.Round((double)p.Amount.Value / 76700.0) : 0));

                int currentMarkedAsPaid = application.Capabilities.Count(c => c.IsAppliedForEmpanelment && c.IsPaid);

                if (totalUnitsPaidInHistory > currentMarkedAsPaid)
                {
                    var unpaidButShouldBePaid = application.Capabilities
                        .Where(c => c.IsAppliedForEmpanelment && !c.IsPaid)
                        .OrderBy(c => c.Id)
                        .Take(totalUnitsPaidInHistory - currentMarkedAsPaid)
                        .ToList();
                    
                    foreach(var d in unpaidButShouldBePaid) { d.IsPaid = true; }
                    await _context.SaveChangesAsync();
                }
            }

            var appFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.AppFee);
            var empFee = application.Payments.FirstOrDefault(p => p.Type == PaymentType.EmpFee);

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
                if (!supplementalId.HasValue) paymentDetail.APCDTypesCount = empFee.APCDTypesCount ?? 0;
            }

            // Initial calculation for Draft
            if (application.Status == "Draft" && !supplementalId.HasValue)
            {
                paymentDetail.Amount = total;
                paymentDetail.APCDTypesCount = currentApcdCount;
                if (paymentDetail.AppFeeAmountDeposited == 0) paymentDetail.AppFeeAmountDeposited = appFeeTotal;
                if (paymentDetail.EmpFeeAmountDeposited == 0) paymentDetail.EmpFeeAmountDeposited = empFeeTotal;
            }

            ViewBag.JsonDocuments = application.Documents.Select(d => new { d.DocumentType, d.AssociatedTech, d.FileName, d.FilePath }).ToList();
            return View(paymentDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSupplementalPayment(int id, int supplementalId, PaymentViewModel payment)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            var supRequest = await _context.SupplementalRequests
                .Include(r => r.Payments)
                .Include(r => r.Devices)
                .FirstOrDefaultAsync(r => r.Id == supplementalId && r.ApplicationId == id);
            if (supRequest == null) return NotFound();

            var supPay = supRequest.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
            if (supPay == null)
            {
                supPay = new SupplementalPayment { SupplementalRequestId = supplementalId };
                _context.SupplementalPayments.Add(supPay);
            }

            // Calculations
            int count = supRequest.Devices.Count;
            decimal baseAmount = count * 65000;
            decimal gstAmount = baseAmount * 0.18m;

            supPay.Amount = baseAmount;
            supPay.GST = gstAmount;
            supPay.TotalAmount = baseAmount + gstAmount;
            supPay.AmountDeposited = payment.SupplementalAmountDeposited ?? 0;
            supPay.BankName = payment.SupplementalBankName ?? string.Empty;
            supPay.UTRNumber = payment.SupplementalUTR ?? string.Empty;
            supPay.PaymentDate = payment.SupplementalPayDate ?? DateTime.UtcNow;
            supPay.NewlyAddedAPCDCount = count;
            supPay.NewlyAddedAPCDTypes = string.Join(", ", supRequest.Devices.Select(d => d.SubTech));
            supPay.UpdatedAt = DateTime.UtcNow;

            // File Upload
            var proofFile = Request.Form.Files["supplementalReceiptFile"];
            if (proofFile != null && proofFile.Length > 0)
            {
                string folder = Path.Combine(_environment.WebRootPath, "uploads", "applications", id.ToString(), "supplemental", supplementalId.ToString(), "payments");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = $"PaymentProof_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(proofFile.FileName)}";
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofFile.CopyToAsync(stream);
                }

                supPay.ReceiptPath = $"/uploads/applications/{id}/supplemental/{supplementalId}/payments/{fileName}";
            }

            await _context.SaveChangesAsync();

            string deviceNames = supPay.NewlyAddedAPCDTypes;
            TempData["SuccessMessage"] = $"{deviceNames} added successfully. Payment details saved successfully on {DateTime.Now:dd MMM yyyy, hh:mm tt}.";

            return RedirectToAction("Payment", new { id, supplementalId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(int id, PaymentViewModel payment, int? supplementalId = null)
        {
            string oemFolder = string.Empty;
            var application = await _context.Applications.Include(a => a.Payments).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            if (supplementalId.HasValue)
            {
                var supRequest = await _context.SupplementalRequests
                    .Include(r => r.Payments)
                    .Include(r => r.Devices)
                    .FirstOrDefaultAsync(r => r.Id == supplementalId && r.ApplicationId == id);
                
                if (supRequest == null) return NotFound();

                // Save Supplemental Payment
                var supPay = supRequest.Payments.FirstOrDefault();
                if (supPay == null)
                {
                    supPay = new SupplementalPayment { SupplementalRequestId = supplementalId.Value };
                    _context.SupplementalPayments.Add(supPay);
                }

                supPay.Amount = payment.SupplementalAmount ?? 0;
                supPay.GST = supPay.Amount * 0.18m / 1.18m;
                supPay.TotalAmount = payment.SupplementalAmount ?? 0;
                supPay.UTRNumber = payment.SupplementalUTR ?? string.Empty;
                supPay.PaymentDate = payment.SupplementalPayDate ?? DateTime.UtcNow;
                supPay.Status = "Submitted";

                // Save Proof
                var proofFile = Request.Form.Files["supplementalReceiptFile"];
                if (proofFile != null && proofFile.Length > 0)
                {
                    string subPath = $"applications/{id}/supplemental/{supplementalId}/PaymentProof";
                    supPay.ReceiptPath = await SaveFileAsync(proofFile, subPath);
                }

                supRequest.Status = "Submitted";
                supRequest.IsFinalSubmitted = true;
                supRequest.FinalSubmittedAt = DateTime.UtcNow;
                
                // Log transaction
                _context.SupplementalTransactionHistories.Add(new SupplementalTransactionHistory {
                    ApplicationId = id,
                    SupplementalRequestId = supplementalId.Value,
                    Action = "Final Submit",
                    Description = $"Submitted Add More APCD request with {supRequest.Devices.Count} devices.",
                    ActionBy = User.Identity.Name ?? "OEM"
                });

                await _context.SaveChangesAsync();
                return RedirectToAction("Submit", new { id, isSupplemental = true });
            }

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
                    string receiptPath = string.Empty;
                    var supFile = Request.Form.Files["supplementalReceiptFile"];
                    if (supFile != null && supFile.Length > 0)
                    {
                        oemFolder = await GetOEMFolderName(id);
                        receiptPath = await SaveFileAsync(supFile, oemFolder);
                    }

                    var suppFee = new Payment 
                    { 
                        ApplicationId = id, 
                        Type = PaymentType.Supplemental,
                        IsSupplemental = true,
                        Amount = payment.SupplementalAmount.Value,
                        UTRNumber = payment.SupplementalUTR,
                        RemitterBank = "Unknown",
                        PaymentDate = payment.SupplementalPayDate ?? DateTime.UtcNow,
                        Status = "Pending",
                        ReceiptPath = receiptPath
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

            oemFolder = await GetOEMFolderName(id);
            await ProcessFileUpload(id, "paymentReceiptFile", "PaymentReceipt", oemFolder, 5, "Common");
            await ProcessFileUpload(id, "supplementalReceiptFile", "SupplementalPaymentReceipt", oemFolder, 5, "Common");

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
        public async Task<IActionResult> Submit(int id, bool isSupplemental = false)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            ViewBag.IsSubmitted = application.Status != "Draft";
            ViewBag.ActualStep = 8;
            ViewBag.IsSupplemental = isSupplemental;
            
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
