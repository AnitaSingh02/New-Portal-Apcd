using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APCD.Web.Migrations
{
    public partial class PaymentFieldsUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "APCDTypesCount",
                table: "PaymentDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AppFeeAmountDeposited",
                table: "PaymentDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppFeePaymentDate",
                table: "PaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppFeeRemitterBank",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AppFeeUTRNumber",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EmpFeeAmountDeposited",
                table: "PaymentDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmpFeePaymentDate",
                table: "PaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpFeeRemitterBank",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmpFeeUTRNumber",
                table: "PaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "APCDTypesCount",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "AppFeeAmountDeposited",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "AppFeePaymentDate",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "AppFeeRemitterBank",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "AppFeeUTRNumber",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "EmpFeeAmountDeposited",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "EmpFeePaymentDate",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "EmpFeeRemitterBank",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "EmpFeeUTRNumber",
                table: "PaymentDetails");
        }
    }
}
