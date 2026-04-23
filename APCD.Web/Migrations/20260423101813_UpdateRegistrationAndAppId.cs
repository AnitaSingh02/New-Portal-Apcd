using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class UpdateRegistrationAndAppId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropTable(
            //     name: "PaymentDetails");

            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GSTNumber",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            // migrationBuilder.AddColumn<string>(
            //     name: "ResetPasswordToken",
            //     table: "Users",
            //     type: "nvarchar(max)",
            //     nullable: true);

            // migrationBuilder.AddColumn<DateTime>(
            //     name: "ResetPasswordTokenExpiry",
            //     table: "Users",
            //     type: "datetime2",
            //     nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationId",
                table: "Applications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // migrationBuilder.AddColumn<string>(
            //     name: "AssociatedTech",
            //     table: "ApplicationDocuments",
            //     type: "nvarchar(max)",
            //     nullable: false,
            //     defaultValue: "");

            // migrationBuilder.CreateTable(
            //     name: "Payments",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         ApplicationId = table.Column<int>(type: "int", nullable: false),
            //         Type = table.Column<string>(type: "varchar(50)", nullable: false),
            //         Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         UTRNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //         RemitterBank = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            //         PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            //         Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            //         ReceiptPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
            //         APCDTypesCount = table.Column<int>(type: "int", nullable: true),
            //         CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Payments", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_Payments_Applications_ApplicationId",
            //             column: x => x.ApplicationId,
            //             principalTable: "Applications",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            // migrationBuilder.CreateIndex(
            //     name: "IX_Payments_ApplicationId",
            //     table: "Payments",
            //     column: "ApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GSTNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetPasswordToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetPasswordTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssociatedTech",
                table: "ApplicationDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "PaymentDetails",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    APCDTypesCount = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppFeeAmountDeposited = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppFeePaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppFeeRemitterBank = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppFeeUTRNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmpFeeAmountDeposited = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmpFeePaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpFeeRemitterBank = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmpFeeUTRNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemitterBank = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UTRNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDetails", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_PaymentDetails_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
