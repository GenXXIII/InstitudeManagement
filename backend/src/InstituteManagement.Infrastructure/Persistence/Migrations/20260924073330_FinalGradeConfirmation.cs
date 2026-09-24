using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalGradeConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAtUtc",
                table: "GradeRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_FinalizedAtUtc_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "FinalizedAtUtc", "AcademicYear", "Term" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_FinalizedAtUtc_AcademicYear_Term",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "FinalizedAtUtc",
                table: "GradeRecords");
        }
    }
}
