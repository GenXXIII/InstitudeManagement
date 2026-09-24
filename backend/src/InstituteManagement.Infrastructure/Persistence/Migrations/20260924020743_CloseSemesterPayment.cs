using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CloseSemesterPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_ClosedAtUtc_DueOn",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "AcademicYear", "Semester", "Status", "ClosedAtUtc", "DueOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_ClosedAtUtc_DueOn",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "AcademicYear", "Semester", "Status", "DueOn" });
        }
    }
}
