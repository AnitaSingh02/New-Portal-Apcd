using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace APCD.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var host = _configuration["SmtpSettings:Host"];
            var portString = _configuration["SmtpSettings:Port"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"];
            var password = _configuration["SmtpSettings:Password"];

            // If SMTP is firmly configured, use it.
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(portString) && !string.IsNullOrEmpty(fromEmail))
            {
                try 
                {
                    int.TryParse(portString, out int port);
                    using var client = new SmtpClient(host, port)
                    {
                        Credentials = new NetworkCredential(fromEmail, password),
                        EnableSsl = true
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "APCD Portal Support"),
                        Subject = subject,
                        Body = message,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] Failed to send email via SMTP: {ex.Message}");
                    // Fallthrough to the local logger to prevent complete lockout during debug
                }
            }

            // Fallback: Local logger for missing SMTP configuration or Development mode drops
            await MockSendEmailAsync(toEmail, subject, message);
        }

        private async Task MockSendEmailAsync(string toEmail, string subject, string message)
        {
            try 
            {
                var logDirectory = Path.Combine(_env.ContentRootPath, "logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var logFile = Path.Combine(logDirectory, "email_logs.txt");
                var logEntry = $"\n--- EMAIL INTERCEPT (No SMTP) [{DateTime.Now}] ---\n" +
                               $"TO: {toEmail}\n" +
                               $"SUBJECT: {subject}\n" +
                               $"BODY:\n{message}\n" +
                               $"------------------------------------------------------\n";

                await File.AppendAllTextAsync(logFile, logEntry);

                // Emulate visual logging to console for instant visibility to the developer
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n=== [LOCAL EMAIL INTERCEPT] ===");
                Console.WriteLine($"To: {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine(message.Contains("http") ? $"[LINK DETECTED]: {ExtractLink(message)}" : "No absolute URL detected");
                Console.WriteLine("===============================\n");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write local email log: {ex.Message}");
            }
        }

        private string ExtractLink(string message) 
        {
            int start = message.IndexOf("http");
            if (start == -1) return "Hidden Link";
            int end = message.IndexOf("\"", start);
            if (end == -1) end = message.IndexOf(" ", start);
            if (end == -1) end = message.IndexOf("<", start);
            return end == -1 ? message.Substring(start) : message.Substring(start, end - start);
        }
    }
}
