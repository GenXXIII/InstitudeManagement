using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistEnrollmentScopedWorkflowCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordCode",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "TeacherAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                schema: "Enrollment",
                table: "TeacherAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordCode",
                schema: "Enrollment",
                table: "TeacherAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinanceCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "CourseAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                schema: "Enrollment",
                table: "CourseAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordCode",
                schema: "Enrollment",
                table: "CourseAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordCode",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_PublicId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_FinanceCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "FinanceCode",
                unique: true,
                filter: "[FinanceCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_PublicId",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_ResultCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "ResultCode",
                unique: true,
                filter: "[ResultCode] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_PublicId",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_FinanceCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_PublicId",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_ResultCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "RecordCode",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropColumn(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "RecordCode",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "FinanceCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "RecordCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "ResultCode",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropColumn(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "RecordCode",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropColumn(
                name: "HistoryCode",
                schema: "Enrollment",
                table: "ClassroomAssignments");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                schema: "Enrollment",
                table: "ClassroomAssignments");

            migrationBuilder.DropColumn(
                name: "RecordCode",
                schema: "Enrollment",
                table: "ClassroomAssignments");
        }
    }
}
