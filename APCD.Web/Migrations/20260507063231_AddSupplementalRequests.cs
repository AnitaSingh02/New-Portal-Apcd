using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class AddSupplementalRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplementalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsFinalSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    LastCompletedStep = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementalRequests_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplementalTransactionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    SupplementalRequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalTransactionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplementalDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplementalRequestId = table.Column<int>(type: "int", nullable: false),
                    MainType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubTech = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DesignedCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementalDevices_SupplementalRequests_SupplementalRequestId",
                        column: x => x.SupplementalRequestId,
                        principalTable: "SupplementalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplementalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplementalRequestId = table.Column<int>(type: "int", nullable: false),
                    APCDType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementalDocuments_SupplementalRequests_SupplementalRequestId",
                        column: x => x.SupplementalRequestId,
                        principalTable: "SupplementalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplementalPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplementalRequestId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GST = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UTRNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementalPayments_SupplementalRequests_SupplementalRequestId",
                        column: x => x.SupplementalRequestId,
                        principalTable: "SupplementalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplementalDevices_SupplementalRequestId",
                table: "SupplementalDevices",
                column: "SupplementalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementalDocuments_SupplementalRequestId",
                table: "SupplementalDocuments",
                column: "SupplementalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementalPayments_SupplementalRequestId",
                table: "SupplementalPayments",
                column: "SupplementalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementalRequests_ApplicationId",
                table: "SupplementalRequests",
                column: "ApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplementalDevices");

            migrationBuilder.DropTable(
                name: "SupplementalDocuments");

            migrationBuilder.DropTable(
                name: "SupplementalPayments");

            migrationBuilder.DropTable(
                name: "SupplementalTransactionHistories");

            migrationBuilder.DropTable(
                name: "SupplementalRequests");
        }
    }
}
