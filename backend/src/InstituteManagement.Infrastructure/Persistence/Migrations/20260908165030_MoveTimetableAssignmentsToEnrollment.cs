using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveTimetableAssignmentsToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "YearLevel",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "YearLevel",
                table: "ScheduleEntries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassroomId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Semester",
                table: "Courses",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Semester 1");

            migrationBuilder.AddColumn<int>(
                name: "YearLevel",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE enrollment
                SET enrollment.[CourseId] = schedule.[CourseId],
                    enrollment.[TeacherId] = schedule.[TeacherId],
                    enrollment.[ClassroomId] = schedule.[ClassroomId],
                    enrollment.[YearLevel] = schedule.[YearLevel]
                FROM [Enrollment].[TimetableEnrollments] AS enrollment
                INNER JOIN [ScheduleEntries] AS schedule ON schedule.[Id] = enrollment.[ScheduleEntryId]
                WHERE schedule.[CourseId] IS NOT NULL
                  AND schedule.[TeacherId] IS NOT NULL
                  AND schedule.[ClassroomId] IS NOT NULL
                  AND schedule.[YearLevel] IS NOT NULL;

                UPDATE course
                SET course.[YearLevel] = assignment.[YearLevel],
                    course.[Semester] = assignment.[Semester]
                FROM [Courses] AS course
                CROSS APPLY
                (
                    SELECT TOP (1) enrolled.[YearLevel], enrolled.[Semester]
                    FROM [Enrollment].[CourseAssignments] AS enrolled
                    WHERE enrolled.[CourseId] = course.[Id]
                      AND enrolled.[Status] <> N'Removed'
                    ORDER BY enrolled.[UpdatedAtUtc] DESC, enrolled.[CreatedAtUtc] DESC
                ) AS assignment;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_YearLevel_Semester",
                table: "Courses",
                columns: new[] { "YearLevel", "Semester" });

            migrationBuilder.AddForeignKey(
                name: "FK_TimetableEnrollments_Classrooms_ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimetableEnrollments_Courses_CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimetableEnrollments_Teachers_TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimetableEnrollments_Classrooms_ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_TimetableEnrollments_Courses_CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_TimetableEnrollments_Teachers_TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_YearLevel_Semester",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "CourseId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "YearLevel",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "YearLevel",
                table: "Courses");

            migrationBuilder.AlterColumn<int>(
                name: "YearLevel",
                table: "ScheduleEntries",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassroomId",
                table: "ScheduleEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
