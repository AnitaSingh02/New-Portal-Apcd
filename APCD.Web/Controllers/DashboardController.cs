using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APCD.Web.Models;
using APCD.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APCD.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Redirect based on Role
            if (User.IsInRole("ADMIN") || User.IsInRole("SUPER_ADMIN") || User.IsInRole("OFFICER") || 
                User.IsInRole("COMMITTEE") || User.IsInRole("FIELD_VERIFIER") || User.IsInRole("DEALING_HAND"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();
            
            // Auto-cleanup: If an OEM holds a formally submitted application matrix, aggressively wipe any accidental orphaned Drafts crafted by the deprecated UI flaw
            var submittedApp = await _context.Applications
                .Where(a => a.UserId == userId && a.Status != "Draft" && a.Status != "Rejected")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (submittedApp != null)
            {
                var orphanedDrafts = await _context.Applications
                    .Where(a => a.UserId == userId && a.Status == "Draft")
                    .ToListAsync();
                    
                if (orphanedDrafts.Any())
                {
                    _context.Applications.RemoveRange(orphanedDrafts);
                    await _context.SaveChangesAsync();
                }
            }

            var application = await _context.Applications
                .Include(a => a.Payment)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.User = user;
            return View(application);
        }
    }
}
