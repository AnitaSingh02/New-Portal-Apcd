using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APCD.Web.Models;
using APCD.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APCD.Web.Controllers
{
    [Authorize]
    public class OEMController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OEMController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            var userId = GetUserId();
            var application = await _context.Applications
                .Include(a => a.Documents)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (application == null) return Json(new List<object>());

            var docs = application.Documents.Where(d => d.IsActive).Select(d => new
            {
                id = d.Id,
                name = d.DocumentType,
                fileName = d.FileName,
                uploadedAt = d.UploadedAt.ToString("MMM dd, yyyy"),
                version = d.Version,
                status = d.DocumentStatus,
                reason = d.DocumentStatus == "Rejected" ? d.RejectionReason : (d.DocumentStatus == "Verified" ? "Approved" : "Under Review"),
                canUpload = d.DocumentStatus == "Rejected"
            }).ToList();

            return Json(docs);
        }

        [HttpPost]
        public async Task<IActionResult> ReUploadDocument(int documentId, IFormFile file)
        {
            var userId = GetUserId();
            var oldDoc = await _context.ApplicationDocuments
                .Include(d => d.Application)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.Application.UserId == userId);

            if (oldDoc == null) return Json(new { success = false, message = "Document not found." });
            if (oldDoc.DocumentStatus == "Verified") return Json(new { success = false, message = "Document already verified." });

            if (file != null && file.Length > 0)
            {
                // Save file
                var folderName = await GetOEMFolderName(oldDoc.ApplicationId);
                var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", folderName);
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Deactivate old record
                oldDoc.IsActive = false;

                // Create new version
                var newDoc = new ApplicationDocument
                {
                    ApplicationId = oldDoc.ApplicationId,
                    DocumentType = oldDoc.DocumentType,
                    AssociatedTech = oldDoc.AssociatedTech,
                    FileName = file.FileName,
                    FilePath = $"/uploads/{folderName}/{fileName}",
                    UploadedAt = DateTime.UtcNow,
                    ParentDocumentId = oldDoc.ParentDocumentId ?? oldDoc.Id,
                    Version = oldDoc.Version + 1,
                    DocumentStatus = "Pending",
                    IsActive = true
                };

                _context.ApplicationDocuments.Add(newDoc);
                await _context.SaveChangesAsync();

                // Record history
                var history = new DocumentReviewHistory
                {
                    DocumentId = newDoc.Id,
                    Status = "Re-uploaded",
                    ActionBy = User.Identity?.Name ?? "OEM",
                    ActionAt = DateTime.UtcNow,
                    RejectionReason = "New version uploaded by OEM"
                };
                _context.DocumentReviewHistories.Add(history);
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Document re-uploaded successfully." });
            }

            return Json(new { success = false, message = "Invalid file." });
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentHistory(int documentId)
        {
            var userId = GetUserId();
            var doc = await _context.ApplicationDocuments
                .Include(d => d.Application)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.Application.UserId == userId);

            if (doc == null) return Json(new { success = false, message = "Document not found." });

            // Get all versions of this document
            var rootId = doc.ParentDocumentId ?? doc.Id;
            var history = await _context.DocumentReviewHistories
                .Include(h => h.Document)
                .Where(h => h.Document.Id == rootId || h.Document.ParentDocumentId == rootId)
                .OrderByDescending(h => h.ActionAt)
                .Select(h => new
                {
                    version = h.Document.Version,
                    status = h.Status,
                    reason = h.RejectionReason,
                    type = h.RejectionType,
                    date = h.ActionAt.ToString("MMM dd, yyyy HH:mm"),
                    by = h.ActionBy
                })
                .ToListAsync();

            return Json(new { success = true, history });
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
    }
}
