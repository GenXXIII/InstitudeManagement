using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkWorkflowCodesToManagementIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql("""
                DECLARE @separator nvarchar(1) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'codeSeparator'), '-');
                DECLARE @padding int = COALESCE(TRY_CONVERT(int, (SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'codePaddingWidth')), 1);
                SET @padding = CASE WHEN @padding < 1 THEN 1 WHEN @padding > 12 THEN 12 ELSE @padding END;

                DECLARE @studentPrefix nvarchar(16) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'studentEnrollmentPrefix'), 'ESTU');
                DECLARE @teacherPrefix nvarchar(16) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'teacherEnrollmentPrefix'), 'ETEA');
                DECLARE @coursePrefix nvarchar(16) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'courseEnrollmentPrefix'), 'ECOU');
                DECLARE @classroomPrefix nvarchar(16) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'classroomEnrollmentPrefix'), 'ECLA');
                DECLARE @timetablePrefix nvarchar(16) = COALESCE((SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = 'code-formats' AND [Key] = 'timetableEnrollmentPrefix'), 'ETIM');

                UPDATE enrollment SET [EnrollmentCode] = student.[StudentCode] + @separator + @studentPrefix + @separator
                    + CASE WHEN LEN([sequence].[Value]) >= @padding THEN [sequence].[Value] ELSE REPLICATE('0', @padding - LEN([sequence].[Value])) + [sequence].[Value] END
                FROM [Enrollment].[StudentEnrollments] enrollment
                INNER JOIN [Students] student ON student.[Id] = enrollment.[StudentId]
                CROSS APPLY (SELECT RIGHT(student.[StudentCode], PATINDEX('%[^0-9]%', REVERSE(student.[StudentCode]) + 'X') - 1) AS [Value]) [sequence];

                UPDATE assignment SET [EnrollmentCode] = teacher.[TeacherCode] + @separator + @teacherPrefix + @separator
                    + CASE WHEN LEN([sequence].[Value]) >= @padding THEN [sequence].[Value] ELSE REPLICATE('0', @padding - LEN([sequence].[Value])) + [sequence].[Value] END
                FROM [Enrollment].[TeacherAssignments] assignment
                INNER JOIN [Teachers] teacher ON teacher.[Id] = assignment.[TeacherId]
                CROSS APPLY (SELECT RIGHT(teacher.[TeacherCode], PATINDEX('%[^0-9]%', REVERSE(teacher.[TeacherCode]) + 'X') - 1) AS [Value]) [sequence];

                UPDATE assignment SET [EnrollmentCode] = course.[CourseCode] + @separator + @coursePrefix + @separator
                    + CASE WHEN LEN([sequence].[Value]) >= @padding THEN [sequence].[Value] ELSE REPLICATE('0', @padding - LEN([sequence].[Value])) + [sequence].[Value] END
                FROM [Enrollment].[CourseAssignments] assignment
                INNER JOIN [Courses] course ON course.[Id] = assignment.[CourseId]
                CROSS APPLY (SELECT RIGHT(course.[CourseCode], PATINDEX('%[^0-9]%', REVERSE(course.[CourseCode]) + 'X') - 1) AS [Value]) [sequence];

                UPDATE assignment SET [EnrollmentCode] = classroom.[ClassroomCode] + @separator + @classroomPrefix + @separator
                    + CASE WHEN LEN([sequence].[Value]) >= @padding THEN [sequence].[Value] ELSE REPLICATE('0', @padding - LEN([sequence].[Value])) + [sequence].[Value] END
                FROM [Enrollment].[ClassroomAssignments] assignment
                INNER JOIN [Classrooms] classroom ON classroom.[Id] = assignment.[ClassroomId]
                CROSS APPLY (SELECT RIGHT(classroom.[ClassroomCode], PATINDEX('%[^0-9]%', REVERSE(classroom.[ClassroomCode]) + 'X') - 1) AS [Value]) [sequence];

                UPDATE enrollment SET [EnrollmentCode] = schedule.[TimetableCode] + @separator + @timetablePrefix + @separator
                    + CASE WHEN LEN([sequence].[Value]) >= @padding THEN [sequence].[Value] ELSE REPLICATE('0', @padding - LEN([sequence].[Value])) + [sequence].[Value] END
                FROM [Enrollment].[TimetableEnrollments] enrollment
                INNER JOIN [ScheduleEntries] schedule ON schedule.[Id] = enrollment.[ScheduleEntryId]
                CROSS APPLY (SELECT RIGHT(schedule.[TimetableCode], PATINDEX('%[^0-9]%', REVERSE(schedule.[TimetableCode]) + 'X') - 1) AS [Value]) [sequence];

                UPDATE [SystemSettings] SET [Value] = 'OPE'
                WHERE [Section] = 'code-formats' AND [Key] LIKE '%OperationPrefix' AND [Value] IN ('OSTU','OTEA','ODEP','OCOU','OCLA','OTIM','OATT','OGRD','OSES');
                UPDATE [SystemSettings] SET [Value] = 'REC'
                WHERE [Section] = 'code-formats' AND [Key] LIKE '%RecordPrefix' AND [Value] IN ('RSTU','RTEA','RDEP','RCOU','RCLA','RTIM','RATT','RGRD','RSES');
                UPDATE [SystemSettings] SET [Value] = 'HIS'
                WHERE [Section] = 'code-formats' AND [Key] LIKE '%HistoryPrefix' AND [Value] IN ('HSTU','HTEA','HDEP','HCOU','HCLA','HTIM','HATT','HGRD','HSES');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Enrollment].[StudentEnrollments] SET [EnrollmentCode] = CONCAT([EnrollmentCode], '-', LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''), 8));
                UPDATE [Enrollment].[TeacherAssignments] SET [EnrollmentCode] = CONCAT([EnrollmentCode], '-', LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''), 8));
                UPDATE [Enrollment].[CourseAssignments] SET [EnrollmentCode] = CONCAT([EnrollmentCode], '-', LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''), 8));
                UPDATE [Enrollment].[ClassroomAssignments] SET [EnrollmentCode] = CONCAT([EnrollmentCode], '-', LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''), 8));
                UPDATE [Enrollment].[TimetableEnrollments] SET [EnrollmentCode] = CONCAT([EnrollmentCode], '-', LEFT(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''), 8));
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
    }
}
