using Microsoft.AspNetCore.Mvc;
using APCD.Web.Models;
using APCD.Web.Data;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace APCD.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(ApplicationUser user, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ViewBag.ErrorMessage = "Email already registered.";
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Role = "OEM";
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LogoutSuccess");
        }

        [HttpGet]
        public IActionResult LogoutSuccess()
        {
            return View("Logout");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
