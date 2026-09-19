using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceDeclarationsAndAnnualCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredAmount",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeclaredAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentPlan",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Semester");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Finance].[FinancialAccounts]
                SET [Title] = CONCAT([Semester], N' payment'),
                    [PaymentPlan] = N'Semester',
                    [DeclaredAmount] = [TuitionFee] + [OtherFee],
                    [DeclaredAtUtc] = [CreatedAtUtc],
                    [ExpiresAtUtc] = DATEADD(day, 1, CAST([DueOn] AS datetime2));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclaredAmount",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "DeclaredAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "PaymentPlan",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "Finance",
                table: "FinancialAccounts");
        }
    }
}
