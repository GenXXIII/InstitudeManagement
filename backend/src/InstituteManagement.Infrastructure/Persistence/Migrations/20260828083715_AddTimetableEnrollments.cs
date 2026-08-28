using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetableEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimetableEnrollments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableEnrollments_ScheduleEntries_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "ScheduleEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_ScheduleEntryId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "ScheduleEntryId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.Sql("""
                DECLARE @AcademicYear nvarchar(32) = COALESCE(
                    (SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = N'academic-year' AND [Key] = N'currentYear'),
                    N'2026–2027');
                DECLARE @Semester nvarchar(32) = COALESCE(
                    (SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = N'semester' AND [Key] = N'currentTerm'),
                    N'Semester 1');

                INSERT INTO [Enrollment].[TimetableEnrollments]
                    ([Id], [ScheduleEntryId], [AcademicYear], [Semester], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT NEWID(), [Id], @AcademicYear, @Semester, N'Active', [CreatedAtUtc], [UpdatedAtUtc]
                FROM [ScheduleEntries]
                WHERE [Status] <> N'Cancelled';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimetableEnrollments",
                schema: "Enrollment");
        }
    }
}
