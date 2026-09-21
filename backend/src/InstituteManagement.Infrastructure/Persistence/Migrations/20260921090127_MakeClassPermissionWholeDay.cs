using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeClassPermissionWholeDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassPermissionRequests_ScheduleEntries_ScheduleEntryId",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_ScheduleEntryId_StudentId_SessionDate",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_StudentId",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_TeacherId_SessionDate_Status",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropColumn(
                name: "ScheduleEntryId",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql(
                """
                WITH RankedPermissions AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY [StudentId], [SessionDate]
                               ORDER BY CASE [Status]
                                            WHEN 'Approved' THEN 0
                                            WHEN 'Pending' THEN 1
                                            ELSE 2
                                        END,
                                        [RequestedAtUtc] DESC
                           ) AS [RowNumber]
                    FROM [Attendance].[ClassPermissionRequests]
                )
                DELETE FROM [Attendance].[ClassPermissionRequests]
                WHERE [Id] IN (
                    SELECT [Id]
                    FROM RankedPermissions
                    WHERE [RowNumber] > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_SessionDate_Status",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                columns: new[] { "SessionDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_StudentId_SessionDate",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                columns: new[] { "StudentId", "SessionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissionRequests_TeacherId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Whole-day requests cannot be mapped reliably back to one schedule entry.
            migrationBuilder.Sql(
                "DELETE FROM [Attendance].[ClassPermissionRequests];");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_SessionDate_Status",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_StudentId_SessionDate",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ClassPermissionRequests_TeacherId",
                schema: "Attendance",
                table: "ClassPermissionRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleEntryId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.AddForeignKey(
                name: "FK_ClassPermissionRequests_ScheduleEntries_ScheduleEntryId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                column: "ScheduleEntryId",
                principalTable: "ScheduleEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
