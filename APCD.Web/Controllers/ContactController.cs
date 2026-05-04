using Microsoft.AspNetCore.Mvc;
using APCD.Web.Models;
using APCD.Web.Data;
using System.Threading.Tasks;
using System;

namespace APCD.Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactUs model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.CompanyName != null)
                    {
                        model.CompanyName = model.CompanyName.Trim();
                    }
                    model.CreateTime = DateTime.Now;
                    _context.ContactUs.Add(model);
                    await _context.SaveChangesAsync();
                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Your message has been submitted successfully. We will get back to you soon.";
                    return View();
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving your message. Please try again later.";
                }
            }
            return View(model);
        }
    }
}
