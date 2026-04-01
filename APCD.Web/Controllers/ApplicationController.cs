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
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == "Draft");

            if (application == null)
            {
                // Check if profile exists
                var profile = await _context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    return RedirectToAction("Profile", "Application");
                }

                // Create new draft
                application = new EmpanelmentApplication { UserId = userId, Status = "Draft", SelectedAPCDCategories = "" };
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Step1", new { id = application.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            return View(profile ?? new CompanyProfile { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> Profile(CompanyProfile profile)
        {
            var userId = GetUserId();
            profile.UserId = userId;
            profile.UpdatedAt = DateTime.UtcNow;

            if (await _context.CompanyProfiles.AnyAsync(p => p.UserId == userId))
            {
                _context.CompanyProfiles.Update(profile);
            }
            else
            {
                _context.CompanyProfiles.Add(profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Step1(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();
            
            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> Step1(int id, string[] categories)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            application.SelectedAPCDCategories = string.Join(",", categories);
            await _context.SaveChangesAsync();

            return RedirectToAction("Step2", new { id = application.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Step2(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(int id, string type, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty");

            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploads = Path.Combine(webRootPath, "uploads", id.ToString());
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"{type}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var doc = new ApplicationDocument
            {
                ApplicationId = id,
                DocumentType = type,
                FileName = file.FileName,
                FilePath = $"/uploads/{id}/{fileName}"
            };

            _context.ApplicationDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return RedirectToAction("Step2", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Step3(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Installations)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> AddInstallation(int id, InstallationRecord record)
        {
            record.Id = 0; // Reset Id to prevent model binder from mapping route 'id' to it
            record.ApplicationId = id;
            _context.InstallationRecords.Add(record);
            await _context.SaveChangesAsync();
            return RedirectToAction("Step3", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Documents)
                .Include(a => a.Installations)
                .Include(a => a.StaffDetails)
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (application == null || application.UserId != GetUserId()) return NotFound();

            // Dynamic Fee Calculation as per official structure
            // Base Fee: 25,000, Per Category: 65,000, GST: 18%
            int categoryCount = (application.SelectedAPCDCategories ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            decimal baseFee = 25000;
            decimal productFees = categoryCount * 65000;
            decimal subtotal = baseFee + productFees;
            decimal gst = subtotal * 0.18m;
            decimal total = subtotal + gst;

            ViewBag.CategoryCount = categoryCount;
            ViewBag.BaseFee = baseFee;
            ViewBag.ProductFees = productFees;
            ViewBag.GST = gst;
            ViewBag.Total = total;

            return View(application.Payment ?? new PaymentDetail { ApplicationId = id, Amount = total });
        }

        [HttpPost]
        public async Task<IActionResult> Payment(int id, PaymentDetail payment)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            payment.ApplicationId = id;
            payment.PaymentDate = DateTime.UtcNow;
            payment.Status = "Pending";

            if (await _context.PaymentDetails.AnyAsync(p => p.ApplicationId == id))
            {
                _context.PaymentDetails.Update(payment);
            }
            else
            {
                _context.PaymentDetails.Add(payment);
            }

            // Automatically finalize submission since Payment is the last step
            application.Status = "Submitted";
            application.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Submit", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Submit(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Payment)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.UserId != GetUserId()) return NotFound();

            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int id, bool confirm = true)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || application.UserId != GetUserId()) return NotFound();

            application.Status = "Submitted";
            application.SubmittedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Submit", new { id });
        }
    }
}
