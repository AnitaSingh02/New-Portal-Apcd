using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddDocumentCategorization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentCategory",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                table: "ApplicationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentCategory",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                table: "ApplicationDocuments");
        }
    }
}
