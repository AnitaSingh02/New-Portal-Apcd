using Microsoft.EntityFrameworkCore;
using APCD.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using APCD.Web.Models;
using APCD.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Core Infrastructure
builder.Services.AddTransient<IEmailService, EmailService>();

// Force session tokens to be completely obliterated anytime the application restarts
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

var app = builder.Build();

// Ensure uploads directory exists
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);

var uploadsPath = Path.Combine(webRootPath, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

// Basic Seed Logic
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try {
        if (!context.Users.Any())
        {
            var seedUsers = new List<ApplicationUser>
            {
                new ApplicationUser { 
                    FullName = "Super Administrator", 
                    Email = "admin@npcindia.gov.in", 
                    PasswordHash = "$2a$11$l3J3tkX28c6DlsZZbw.4Tu/sv6zMTIIsZhPLzTz6byei.sW7J7dga", 
                    Role = "SUPER_ADMIN", 
                    MobileNumber = "0000000000" 
                },
                new ApplicationUser { 
                    FullName = "Head Administrator", 
                    Email = "head@npcindia.gov.in", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Head@APCD2025!"), 
                    Role = "ADMIN", 
                    MobileNumber = "0000000001" 
                },
                new ApplicationUser { 
                    FullName = "NPC Officer", 
                    Email = "officer@npcindia.gov.in", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Officer@APCD2025!"), 
                    Role = "OFFICER", 
                    MobileNumber = "0000000002" 
                },
                new ApplicationUser { 
                    FullName = "Committee Member", 
                    Email = "committee@npcindia.gov.in", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Committee@APCD2025!"), 
                    Role = "COMMITTEE", 
                    MobileNumber = "0000000003" 
                },
                new ApplicationUser { 
                    FullName = "Field Verifier", 
                    Email = "fieldverifier@npcindia.gov.in", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@APCD2025!"), 
                    Role = "FIELD_VERIFIER", 
                    MobileNumber = "0000000004" 
                },
                new ApplicationUser { 
                    FullName = "Dealing Hand", 
                    Email = "dealinghand@npcindia.gov.in", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dealing@APCD2025!"), 
                    Role = "DEALING_HAND", 
                    MobileNumber = "0000000005" 
                },
                new ApplicationUser { 
                    FullName = "Test OEM", 
                    Email = "oem@testcompany.com", 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Oem@APCD2025!"), 
                    Role = "OEM", 
                    MobileNumber = "0000000006" 
                }
            };
            context.Users.AddRange(seedUsers);
            context.SaveChanges();
        }

        // --- NEW: Database Schema Fix for Remarks and Payments ---
        var sqlRemarks = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ApplicationRemarks]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [ApplicationRemarks] (
                    [Id] int NOT NULL IDENTITY,
                    [ApplicationId] int NOT NULL,
                    [Role] nvarchar(max) NOT NULL,
                    [UserName] nvarchar(max) NOT NULL,
                    [Comment] nvarchar(max) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_ApplicationRemarks] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ApplicationRemarks_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE CASCADE
                );
            END";

        var sqlPayment = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PaymentDetails]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [PaymentDetails] (
                    [ApplicationId] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [UTRNumber] nvarchar(100) NOT NULL,
                    [PaymentDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [Status] nvarchar(50) NOT NULL DEFAULT 'Pending',
                    CONSTRAINT [PK_PaymentDetails] PRIMARY KEY ([ApplicationId]),
                    CONSTRAINT [FK_PaymentDetails_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE CASCADE
                );
            END";

        var sqlAddResetTokens = @"
            IF COL_LENGTH('Users', 'ResetPasswordToken') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [ResetPasswordToken] nvarchar(max) NULL;
            END

            IF COL_LENGTH('Users', 'ResetPasswordTokenExpiry') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [ResetPasswordTokenExpiry] datetime2 NULL;
            END";

        var sqlAddPaymentCols = @"
            IF COL_LENGTH('PaymentDetails', 'AppFeeAmountDeposited') IS NULL ALTER TABLE [PaymentDetails] ADD [AppFeeAmountDeposited] decimal(18,2) NOT NULL DEFAULT 0;
            IF COL_LENGTH('PaymentDetails', 'AppFeeRemitterBank') IS NULL ALTER TABLE [PaymentDetails] ADD [AppFeeRemitterBank] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'AppFeeUTRNumber') IS NULL ALTER TABLE [PaymentDetails] ADD [AppFeeUTRNumber] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'AppFeePaymentDate') IS NULL ALTER TABLE [PaymentDetails] ADD [AppFeePaymentDate] datetime2 NULL;
            IF COL_LENGTH('PaymentDetails', 'APCDTypesCount') IS NULL ALTER TABLE [PaymentDetails] ADD [APCDTypesCount] int NOT NULL DEFAULT 0;
            IF COL_LENGTH('PaymentDetails', 'EmpFeeAmountDeposited') IS NULL ALTER TABLE [PaymentDetails] ADD [EmpFeeAmountDeposited] decimal(18,2) NOT NULL DEFAULT 0;
            IF COL_LENGTH('PaymentDetails', 'EmpFeeRemitterBank') IS NULL ALTER TABLE [PaymentDetails] ADD [EmpFeeRemitterBank] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'EmpFeeUTRNumber') IS NULL ALTER TABLE [PaymentDetails] ADD [EmpFeeUTRNumber] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'EmpFeePaymentDate') IS NULL ALTER TABLE [PaymentDetails] ADD [EmpFeePaymentDate] datetime2 NULL;
            IF COL_LENGTH('PaymentDetails', 'SupplementalAmount') IS NULL ALTER TABLE [PaymentDetails] ADD [SupplementalAmount] decimal(18,2) NULL;
            IF COL_LENGTH('PaymentDetails', 'SupplementalUTR') IS NULL ALTER TABLE [PaymentDetails] ADD [SupplementalUTR] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'SupplementalReceiptPath') IS NULL ALTER TABLE [PaymentDetails] ADD [SupplementalReceiptPath] nvarchar(max) NOT NULL DEFAULT '';
            IF COL_LENGTH('PaymentDetails', 'SupplementalPayDate') IS NULL ALTER TABLE [PaymentDetails] ADD [SupplementalPayDate] datetime2 NULL;
        ";

        context.Database.ExecuteSqlRaw(sqlRemarks);
        context.Database.ExecuteSqlRaw(sqlPayment);
        context.Database.ExecuteSqlRaw(sqlAddResetTokens);
        context.Database.ExecuteSqlRaw(sqlAddPaymentCols);
        // ---------------------------------------------------------
    } catch (Exception ex) {
        // Log error if table doesn't exist yet
        Console.WriteLine("Seed Error: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
