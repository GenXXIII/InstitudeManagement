using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentBusinessCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "TeacherAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "CourseAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                WITH Codes AS (
                    SELECT EnrollmentCode, ROW_NUMBER() OVER (ORDER BY Id) AS Sequence
                    FROM [Enrollment].[TimetableEnrollments]
                ) UPDATE Codes SET EnrollmentCode = CONCAT('ETIM-', Sequence);
                WITH Codes AS (
                    SELECT EnrollmentCode, ROW_NUMBER() OVER (ORDER BY Id) AS Sequence
                    FROM [Enrollment].[TeacherAssignments]
                ) UPDATE Codes SET EnrollmentCode = CONCAT('ETEA-', Sequence);
                WITH Codes AS (
                    SELECT EnrollmentCode, ROW_NUMBER() OVER (ORDER BY Id) AS Sequence
                    FROM [Enrollment].[StudentEnrollments]
                ) UPDATE Codes SET EnrollmentCode = CONCAT('ESTU-', Sequence);
                WITH Codes AS (
                    SELECT EnrollmentCode, ROW_NUMBER() OVER (ORDER BY Id) AS Sequence
                    FROM [Enrollment].[CourseAssignments]
                ) UPDATE Codes SET EnrollmentCode = CONCAT('ECOU-', Sequence);
                WITH Codes AS (
                    SELECT EnrollmentCode, ROW_NUMBER() OVER (ORDER BY Id) AS Sequence
                    FROM [Enrollment].[ClassroomAssignments]
                ) UPDATE Codes SET EnrollmentCode = CONCAT('ECLA-', Sequence);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_EnrollmentCode",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "TeacherAssignments",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_EnrollmentCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                column: "EnrollmentCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_EnrollmentCode",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_EnrollmentCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ClassroomAssignments_EnrollmentCode",
                schema: "Enrollment",
                table: "ClassroomAssignments");

            migrationBuilder.DropColumn(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "EnrollmentCode",
                schema: "Enrollment",
                table: "ClassroomAssignments");
        }
    }
}
