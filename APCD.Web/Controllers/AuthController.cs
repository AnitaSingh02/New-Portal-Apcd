using Microsoft.AspNetCore.Mvc;
using APCD.Web.Models;
using APCD.Web.Data;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using APCD.Web.Services;

namespace APCD.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                    IsPersistent = false
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Please enter a valid email address.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // To prevent email enumeration, we return the same view as success even if the email doesn't exist
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Generate cryptographic token
            var rawToken = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = rawToken;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 Hour lifespan
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Construct Reset Link matching this Controller's routing
            var resetLink = Url.Action("ResetPassword", "Auth", new { token = rawToken, email = user.Email }, Request.Scheme);

            // Transmit Email
            string subject = "APCD Portal - Password Reset Request";
            string body = $@"
                <h3>Password Reset Authorized</h3>
                <p>We received a request to reset your APCD OEM Empanelment Portal password.</p>
                <p>Please click the secure link below to reset your password. This link will expire precisely in 1 hour.</p>
                <p><a href='{resetLink}' style='background-color:#0b5ed7; color:white; padding:10px 15px; text-decoration:none; border-radius:5px;'>Reset Password Now</a></p>
                <br/>
                <p><small>If you did not request this, please ignore this email.</small></p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Invalid password reset link.";
                return View("Error");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetPasswordToken == token);
            if (user == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                ViewBag.ErrorMessage = "This password reset token is invalid or has completely expired.";
                return View("Error"); // We can render a graceful error view instead of generic Error if we make one
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetPasswordToken == token);
            if (user == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                ViewBag.ErrorMessage = "Your password reset token has expired. Please request a new one.";
                return View();
            }

            // Encrypt and save new credential vector
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            // Strictly consume the token to prevent malicious replay attacks
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
