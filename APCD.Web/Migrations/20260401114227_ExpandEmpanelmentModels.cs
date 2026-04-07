using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class ExpandEmpanelmentModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "StaffDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "StaffDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StaffType",
                table: "StaffDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UTRNumber",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "RemitterBank",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PerformanceCertPath",
                table: "InstallationRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AreaSqm",
                table: "CompanyProfiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNo",
                table: "CompanyProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeCount",
                table: "CompanyProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmSize",
                table: "CompanyProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirmType",
                table: "CompanyProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Latitude",
                table: "CompanyProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Longitude",
                table: "CompanyProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "CompanyProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlacklistDetails",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DPIITRecognitionNo",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasGrievanceSystem",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ISOStandards",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlacklisted",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocalSupplier",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMSE",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStartup",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UdyamRegistrationNo",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "APCDCapabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    MainType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubTech = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsManufactured = table.Column<bool>(type: "bit", nullable: false),
                    IsAppliedForEmpanelment = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DesignedCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeDetails = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APCDCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APCDCapabilities_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRemarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRemarks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TurnoverRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    FinancialYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AuditCertificatePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnoverRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnoverRecords_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APCDCapabilities_ApplicationId",
                table: "APCDCapabilities",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRemarks_ApplicationId",
                table: "ApplicationRemarks",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnoverRecords_ApplicationId",
                table: "TurnoverRecords",
                column: "ApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APCDCapabilities");

            migrationBuilder.DropTable(
                name: "ApplicationRemarks");

            migrationBuilder.DropTable(
                name: "TurnoverRecords");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "StaffDetails");

            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "StaffDetails");

            migrationBuilder.DropColumn(
                name: "StaffType",
                table: "StaffDetails");

            migrationBuilder.DropColumn(
                name: "RemitterBank",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "PerformanceCertPath",
                table: "InstallationRecords");

            migrationBuilder.DropColumn(
                name: "AreaSqm",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ContactNo",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeeCount",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "FirmSize",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "FirmType",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "State",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "BlacklistDetails",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DPIITRecognitionNo",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "HasGrievanceSystem",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ISOStandards",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsBlacklisted",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsLocalSupplier",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsMSE",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsStartup",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "UdyamRegistrationNo",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "UTRNumber",
                table: "PaymentDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
