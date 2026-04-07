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
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == "Draft");

            if (application == null)
            {
                application = new EmpanelmentApplication { UserId = userId, Status = "Draft" };
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Step1", new { id = application.Id });
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

            var isoStandardsFile = Request.Form.Files["isoStandardsFile"];
            if (isoStandardsFile != null && isoStandardsFile.Length > 0)
            {
                var path = await SaveFileAsync(isoStandardsFile, "Certifications");
                await AddOrUpdateDocument(id, "ISOStandardsCertificate", isoStandardsFile.FileName, path);
            }

            var mseFile = Request.Form.Files["mseFile"];
            if (mseFile != null && mseFile.Length > 0)
            {
                var path = await SaveFileAsync(mseFile, "Certifications");
                await AddOrUpdateDocument(id, "MSECertificate", mseFile.FileName, path);
            }

            var startupFile = Request.Form.Files["startupFile"];
            if (startupFile != null && startupFile.Length > 0)
            {
                var path = await SaveFileAsync(startupFile, "Certifications");
                await AddOrUpdateDocument(id, "StartupCertificate", startupFile.FileName, path);
            }

            var localSupplierFile = Request.Form.Files["localSupplierFile"];
            if (localSupplierFile != null && localSupplierFile.Length > 0)
            {
                var path = await SaveFileAsync(localSupplierFile, "Certifications");
                await AddOrUpdateDocument(id, "LocalSupplierCertificate", localSupplierFile.FileName, path);
            }

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
        #endregion

        #region Step 4: Technical Scope (Points 21, 22)
        [HttpGet]
        public async Task<IActionResult> Step4(int id)
        {
            var capabilities = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            ViewBag.AppId = id;
            return View(capabilities);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCapabilities(int id, List<APCDCapability> capabilities)
        {
            var existing = await _context.APCDCapabilities.Where(c => c.ApplicationId == id).ToListAsync();
            _context.APCDCapabilities.RemoveRange(existing);
            
            foreach(var cap in capabilities)
            {
                if (!string.IsNullOrWhiteSpace(cap.DesignedCapacity))
                {
                    cap.ApplicationId = id;
                    _context.APCDCapabilities.Add(cap);
                }
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

                var certFile = Request.Form.Files[$"TurnoverCert_{year}"];
                if (certFile != null && certFile.Length > 0)
                {
                    var path = await SaveFileAsync(certFile, "TurnoverCerts");
                    await AddOrUpdateDocument(id, $"TurnoverCert_{year}", certFile.FileName, path);
                    var turnover = application.Turnovers.FirstOrDefault(t => t.FinancialYear == year);
                    if(turnover != null) turnover.AuditCertificatePath = path;
                }
            }

            var bankSolvencyFile = Request.Form.Files["bankSolvencyFile"];
            if (bankSolvencyFile != null && bankSolvencyFile.Length > 0)
            {
                var path = await SaveFileAsync(bankSolvencyFile, "BankSolvency");
                await AddOrUpdateDocument(id, "BankSolvency", bankSolvencyFile.FileName, path);
            }

            var testimonialFiles = Request.Form.Files.GetFiles("testimonialFiles");
            if (testimonialFiles != null && testimonialFiles.Count > 0)
            {
                var existingTestimonials = await _context.ApplicationDocuments
                    .Where(d => d.ApplicationId == id && d.DocumentType == "ClientTestimonial")
                    .ToListAsync();
                _context.ApplicationDocuments.RemoveRange(existingTestimonials);

                foreach (var file in testimonialFiles)
                {
                    var path = await SaveFileAsync(file, "Testimonials");
                    _context.ApplicationDocuments.Add(new ApplicationDocument
                    {
                        ApplicationId = id,
                        DocumentType = "ClientTestimonial",
                        FileName = file.FileName,
                        FilePath = path
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Review", new { id });
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folder}/{fileName}";
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

            var paymentDetail = new PaymentDetail 
            { 
                ApplicationId = id, 
                Amount = total,
                APCDTypesCount = apcdCount,
                AppFeeAmountDeposited = appFeeTotal,
                EmpFeeAmountDeposited = empFeeTotal
            };

            return View(paymentDetail);
        }

        [HttpPost]
        public async Task<IActionResult> Payment(int id, PaymentDetail payment)
        {
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
