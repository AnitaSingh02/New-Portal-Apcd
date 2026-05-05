using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddVerifiedAtToDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ApplicationDocuments",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ApplicationDocuments");
        }
    }
}
