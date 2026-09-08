using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_ClassroomId",
                table: "ScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_TeacherId",
                table: "ScheduleEntries");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_AcademicYear_Semester_Status",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "AcademicYear", "Semester", "Status" })
                .Annotation("SqlServer:Include", new[] { "ScheduleEntryId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId" })
                .Annotation("SqlServer:Include", new[] { "TeacherId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "AcademicYear", "Semester", "Status", "Shift", "DepartmentId", "YearLevel" })
                .Annotation("SqlServer:Include", new[] { "StudentId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ClassroomId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries",
                columns: new[] { "ClassroomId", "DayOfWeek", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TeacherId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries",
                columns: new[] { "TeacherId", "DayOfWeek", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_UpdatedAtUtc",
                table: "GradeRecords",
                column: "UpdatedAtUtc")
                .Annotation("SqlServer:Include", new[] { "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_AcademicYear_Semester_Status_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId", "YearLevel" })
                .Annotation("SqlServer:Include", new[] { "CourseId", "TeacherId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_AcademicYear_Term",
                table: "ClassSessionRecords",
                columns: new[] { "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_DepartmentId_YearLevel",
                table: "ClassSessionRecords",
                columns: new[] { "DepartmentId", "YearLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_SessionDate",
                table: "ClassSessionRecords",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId" })
                .Annotation("SqlServer:Include", new[] { "ClassroomId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc",
                descending: new bool[0])
                .Annotation("SqlServer:Include", new[] { "Action", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Type_Action_ResourceId",
                table: "AuditLogs",
                columns: new[] { "Type", "Action", "ResourceId" },
                filter: "[ResourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AcademicYear_Term_CreatedAtUtc",
                table: "AttendanceRecords",
                columns: new[] { "AcademicYear", "Term", "CreatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Date",
                table: "AttendanceRecords",
                column: "Date")
                .Annotation("SqlServer:Include", new[] { "StudentId", "Status", "CheckedInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_AcademicYear_Semester_Status",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TimetableEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TimetableEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_ClassroomId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_TeacherId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_AcademicYear_Term",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_UpdatedAtUtc",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_CourseAssignments_AcademicYear_Semester_Status_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CourseAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "CourseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessionRecords_AcademicYear_Term",
                table: "ClassSessionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessionRecords_DepartmentId_YearLevel",
                table: "ClassSessionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessionRecords_SessionDate",
                table: "ClassSessionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClassroomAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "ClassroomAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ClassroomAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "ClassroomAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Type_Action_ResourceId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_AcademicYear_Term_CreatedAtUtc",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_Date",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ClassroomId",
                table: "ScheduleEntries",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TeacherId",
                table: "ScheduleEntries",
                column: "TeacherId");
        }
    }
}
