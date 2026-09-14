using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSessionStarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Attendance");

            migrationBuilder.CreateTable(
                name: "ClassSessionStarts",
                schema: "Attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessionStarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSessionStarts_ScheduleEntries_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "ScheduleEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessionStarts_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStarts_ScheduleEntryId_SessionDate",
                schema: "Attendance",
                table: "ClassSessionStarts",
                columns: new[] { "ScheduleEntryId", "SessionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStarts_StartedAtUtc",
                schema: "Attendance",
                table: "ClassSessionStarts",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStarts_TeacherId_SessionDate",
                schema: "Attendance",
                table: "ClassSessionStarts",
                columns: new[] { "TeacherId", "SessionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSessionStarts",
                schema: "Attendance");
        }
    }
}
