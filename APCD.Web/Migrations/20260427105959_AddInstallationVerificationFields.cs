using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddInstallationVerificationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCertificateIssued",
                table: "InstallationRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDate",
                table: "InstallationRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "InstallationRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "InstallationRecords",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCertificateIssued",
                table: "InstallationRecords");

            migrationBuilder.DropColumn(
                name: "VerificationDate",
                table: "InstallationRecords");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "InstallationRecords");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "InstallationRecords");
        }
    }
}
