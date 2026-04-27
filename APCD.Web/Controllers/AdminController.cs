using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APCD.Web.Models;
using APCD.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APCD.Web.Controllers
{
    [Authorize(Roles = "ADMIN,SUPER_ADMIN,OFFICER,COMMITTEE,FIELD_VERIFIER,DEALING_HAND")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            IQueryable<EmpanelmentApplication> query = _context.Applications
                .Include(a => a.User)
                .Include(a => a.User.CompanyProfile)
                .Include(a => a.Payments);

            // Server-side filtering
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Id.ToString().Contains(search) || 
                                       a.User.CompanyName.Contains(search) || 
                                       (a.User.CompanyProfile != null && a.User.CompanyProfile.CompanyName.Contains(search)));
            }

            // Role-based filtering for Task Queues
            if (role == "DEALING_HAND")
            {
                query = query.Where(a => a.Status == "Submitted");
            }
            else if (role == "FIELD_VERIFIER")
            {
                query = query.Where(a => a.Status == "ProvisionalGranted");
            }
            else if (role == "OFFICER")
            {
                query = query.Where(a => a.Status == "DocumentApproved" || a.Status == "FieldVerified");
            }
            else if (role == "COMMITTEE")
            {
                query = query.Where(a => a.Status == "CommitteeReviewPending");
            }

            ViewBag.Search = search;
            var applications = await query
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return View(applications);
        }

        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.User.CompanyProfile)
                .Include(a => a.Documents)
                .Include(a => a.Installations)
                .Include(a => a.Payments)
                .Include(a => a.Remarks)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAction(int id, string nextStatus, string comment)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();

            // Add the remark from the current persona
            var remark = new ApplicationRemark
            {
                ApplicationId = id,
                Comment = comment,
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Internal",
                UserName = User.Identity?.Name ?? "Unknown"
            };
            _context.ApplicationRemarks.Add(remark);

            // Transition status
            application.Status = nextStatus;
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDocument(int docId, int appId)
        {
            var doc = await _context.ApplicationDocuments.FirstOrDefaultAsync(d => d.Id == docId && d.ApplicationId == appId);
            if (doc != null)
            {
                doc.IsVerified = !doc.IsVerified;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = appId });
        }

        #region Monitoring Dashboard Views (AJAX)

        [HttpGet]
        public async Task<IActionResult> GetApplicationsList(string status = null)
        {
            var query = _context.Applications
                .Include(a => a.User)
                .Include(a => a.User.CompanyProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            var apps = await query.OrderByDescending(a => a.SubmittedAt).ToListAsync();
            return PartialView("_ApplicationsList", apps);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentsList(string status = null)
        {
            var query = _context.ApplicationDocuments
                .Include(d => d.Application)
                .ThenInclude(a => a.User)
                .AsQueryable();

            if (status == "Verified") query = query.Where(d => d.IsVerified);
            else if (status == "Pending") query = query.Where(d => !d.IsVerified);

            var docs = await query.OrderByDescending(d => d.Id).ToListAsync();
            return PartialView("_DocumentsList", docs);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentsList()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Application)
                        .ThenInclude(a => a.User)
                    .Include(p => p.Application)
                        .ThenInclude(a => a.Capabilities)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return PartialView("_PaymentsList", payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInstallationsList(string state = null)
        {
            var query = _context.InstallationRecords
                .Include(i => i.Application)
                .ThenInclude(a => a.User)
                .Include(a => a.Application.User.CompanyProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(state))
            {
                query = query.Where(i => i.Application.User.CompanyProfile.State == state);
            }

            var installs = await query.OrderByDescending(i => i.Id).ToListAsync();
            return PartialView("_InstallationsList", installs);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmpanelmentStats()
        {
            var apps = await _context.Applications.ToListAsync();
            return PartialView("_EmpanelmentStats", apps);
        }

        [HttpGet]
        public async Task<IActionResult> GetSummarySheet()
        {
            var apps = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Payments)
                .Include(a => a.Capabilities)
                .ToListAsync();
            return PartialView("_SummarySheet", apps);
        }

        #endregion
    }
}
