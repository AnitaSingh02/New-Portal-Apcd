using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddDocumentRejectionFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "ApplicationDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "ApplicationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionType",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "RejectionType",
                table: "ApplicationDocuments");
        }
    }
}
