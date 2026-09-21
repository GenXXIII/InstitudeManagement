using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResultsAndClassPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Grades");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "GradeRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "GradeRecords",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "GradeRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionVersion",
                table: "GradeRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "GradeRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByTeacherId",
                table: "GradeRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassPermissionRequests",
                schema: "Attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPermissionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassPermissionRequests_ScheduleEntries_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "ScheduleEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassPermissionRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassPermissionRequests_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SemesterResultPublications",
                schema: "Grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterResultPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemesterResultPublications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_ReviewStatus_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "ReviewStatus", "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_SubmittedByTeacherId",
                table: "GradeRecords",
                column: "SubmittedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_ScheduleEntryId_StudentId_SessionDate",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                columns: new[] { "ScheduleEntryId", "StudentId", "SessionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_StudentId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_TeacherId_SessionDate_Status",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                columns: new[] { "TeacherId", "SessionDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_PublishedAtUtc",
                schema: "Grades",
                table: "SemesterResultPublications",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_StudentId_AcademicYear_Term",
                schema: "Grades",
                table: "SemesterResultPublications",
                columns: new[] { "StudentId", "AcademicYear", "Term" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Teachers_SubmittedByTeacherId",
                table: "GradeRecords",
                column: "SubmittedByTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Teachers_SubmittedByTeacherId",
                table: "GradeRecords");

            migrationBuilder.DropTable(
                name: "ClassPermissionRequests",
                schema: "Attendance");

            migrationBuilder.DropTable(
                name: "SemesterResultPublications",
                schema: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_ReviewStatus_AcademicYear_Term",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_SubmittedByTeacherId",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "SubmissionVersion",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "SubmittedByTeacherId",
                table: "GradeRecords");
        }
    }
}
