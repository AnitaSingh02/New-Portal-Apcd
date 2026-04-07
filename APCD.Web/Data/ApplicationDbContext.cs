using Microsoft.EntityFrameworkCore;
using APCD.Web.Models;

namespace APCD.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<EmpanelmentApplication> Applications { get; set; }
        public DbSet<InstallationRecord> InstallationRecords { get; set; }
        public DbSet<StaffDetail> StaffDetails { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
        public DbSet<ApplicationRemark> ApplicationRemarks { get; set; }
        public DbSet<TurnoverRecord> TurnoverRecords { get; set; }
        public DbSet<APCDCapability> APCDCapabilities { get; set; }
        public DbSet<PaymentDetail> PaymentDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.CompanyProfile)
                .WithOne(cp => cp.User)
                .HasForeignKey<CompanyProfile>(cp => cp.UserId);

            modelBuilder.Entity<EmpanelmentApplication>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<PaymentDetail>()
                .HasOne(p => p.Application)
                .WithOne(a => a.Payment)
                .HasForeignKey<PaymentDetail>(p => p.ApplicationId);

            modelBuilder.Entity<PaymentDetail>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            base.OnModelCreating(modelBuilder);
        }
    }
}
