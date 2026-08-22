using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TeacherNumber",
                table: "Teachers",
                newName: "TeacherCode");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_TeacherNumber",
                table: "Teachers",
                newName: "IX_Teachers_TeacherCode");

            migrationBuilder.RenameColumn(
                name: "StudentNumber",
                table: "Students",
                newName: "StudentCode");

            migrationBuilder.RenameIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                newName: "IX_Students_StudentCode");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Departments",
                newName: "DepartmentCode");

            migrationBuilder.RenameIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                newName: "IX_Departments_DepartmentCode");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Courses",
                newName: "CourseCode");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                newName: "IX_Courses_CourseCode");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Classrooms",
                newName: "ClassroomCode");

            migrationBuilder.RenameIndex(
                name: "IX_Classrooms_Code",
                table: "Classrooms",
                newName: "IX_Classrooms_ClassroomCode");

            migrationBuilder.AddColumn<string>(
                name: "TimetableCode",
                table: "ScheduleEntries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotificationCode",
                table: "Notifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GradeCode",
                table: "GradeRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClassSessionRecordCode",
                table: "ClassSessionRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuditLogCode",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceCode",
                table: "AttendanceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE [ScheduleEntries] SET [TimetableCode] = 'TIM-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [TimetableCode] = '';");
            migrationBuilder.Sql("UPDATE [Notifications] SET [NotificationCode] = 'NOT-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [NotificationCode] = '';");
            migrationBuilder.Sql("UPDATE [GradeRecords] SET [GradeCode] = 'GRD-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [GradeCode] = '';");
            migrationBuilder.Sql("UPDATE [ClassSessionRecords] SET [ClassSessionRecordCode] = 'CSR-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [ClassSessionRecordCode] = '';");
            migrationBuilder.Sql("UPDATE [AuditLogs] SET [AuditLogCode] = 'AUD-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [AuditLogCode] = '';");
            migrationBuilder.Sql("UPDATE [AttendanceRecords] SET [AttendanceCode] = 'ATT-' + REPLACE(CONVERT(nvarchar(36), [Id]), '-', '') WHERE [AttendanceCode] = '';");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TimetableCode",
                table: "ScheduleEntries",
                column: "TimetableCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationCode",
                table: "Notifications",
                column: "NotificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_GradeCode",
                table: "GradeRecords",
                column: "GradeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_ClassSessionRecordCode",
                table: "ClassSessionRecords",
                column: "ClassSessionRecordCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditLogCode",
                table: "AuditLogs",
                column: "AuditLogCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceCode",
                table: "AttendanceRecords",
                column: "AttendanceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_TimetableCode",
                table: "ScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_NotificationCode",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_GradeCode",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessionRecords_ClassSessionRecordCode",
                table: "ClassSessionRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_AuditLogCode",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_AttendanceCode",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "TimetableCode",
                table: "ScheduleEntries");

            migrationBuilder.DropColumn(
                name: "NotificationCode",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "GradeCode",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ClassSessionRecordCode",
                table: "ClassSessionRecords");

            migrationBuilder.DropColumn(
                name: "AuditLogCode",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AttendanceCode",
                table: "AttendanceRecords");

            migrationBuilder.RenameColumn(
                name: "TeacherCode",
                table: "Teachers",
                newName: "TeacherNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_TeacherCode",
                table: "Teachers",
                newName: "IX_Teachers_TeacherNumber");

            migrationBuilder.RenameColumn(
                name: "StudentCode",
                table: "Students",
                newName: "StudentNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                newName: "IX_Students_StudentNumber");

            migrationBuilder.RenameColumn(
                name: "DepartmentCode",
                table: "Departments",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                newName: "IX_Departments_Code");

            migrationBuilder.RenameColumn(
                name: "CourseCode",
                table: "Courses",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                newName: "IX_Courses_Code");

            migrationBuilder.RenameColumn(
                name: "ClassroomCode",
                table: "Classrooms",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Classrooms_ClassroomCode",
                table: "Classrooms",
                newName: "IX_Classrooms_Code");
        }
    }
}
