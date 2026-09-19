using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBakongKhqrToFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BakongMd5",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BakongQrPayload",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "QrExpiresAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrGeneratedAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BakongMd5",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "BakongQrPayload",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "QrExpiresAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "QrGeneratedAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");
        }
    }
}
