using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddDocumentVersioningAndHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentStatus",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ApplicationDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentDocumentId",
                table: "ApplicationDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ApplicationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DocumentReviewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReviewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentReviewHistories_ApplicationDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "ApplicationDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReviewHistories_DocumentId",
                table: "DocumentReviewHistories",
                column: "DocumentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentReviewHistories");

            migrationBuilder.DropColumn(
                name: "DocumentStatus",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ParentDocumentId",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ApplicationDocuments");
        }
    }
}
