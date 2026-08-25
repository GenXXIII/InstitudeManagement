using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicEnrollmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Enrollment");

            migrationBuilder.CreateTable(
                name: "ClassroomAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Access = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomAssignments_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassroomAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_ClassroomId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                columns: new[] { "ClassroomId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_DepartmentId",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_CourseId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "CourseId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "DepartmentId", "YearLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_TeacherId",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "DepartmentId", "YearLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_DepartmentId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.Sql("""
                DECLARE @AcademicYear nvarchar(32) = COALESCE(
                    (SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = N'academic-year' AND [Key] = N'currentYear'),
                    N'2026–2027');
                DECLARE @Semester nvarchar(32) = COALESCE(
                    (SELECT TOP (1) [Value] FROM [SystemSettings] WHERE [Section] = N'semester' AND [Key] = N'currentTerm'),
                    N'Semester 1');

                INSERT INTO [Enrollment].[StudentEnrollments]
                    ([Id], [StudentId], [DepartmentId], [YearLevel], [Shift], [AcademicYear], [Semester], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT NEWID(), [Id], [DepartmentId], [YearLevel], [Shift], @AcademicYear, @Semester,
                    CASE WHEN [Status] = N'Inactive' THEN N'Paused' ELSE N'Active' END, [CreatedAtUtc], [UpdatedAtUtc]
                FROM [Students];

                INSERT INTO [Enrollment].[TeacherAssignments]
                    ([Id], [TeacherId], [DepartmentId], [AcademicYear], [Semester], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT NEWID(), [Id], [DepartmentId], @AcademicYear, @Semester,
                    CASE WHEN [DepartmentId] IS NULL THEN N'Unassigned' ELSE N'Assigned' END, [CreatedAtUtc], [UpdatedAtUtc]
                FROM [Teachers];

                INSERT INTO [Enrollment].[CourseAssignments]
                    ([Id], [CourseId], [DepartmentId], [TeacherId], [YearLevel], [Capacity], [AcademicYear], [Semester], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT NEWID(), course.[Id], course.[DepartmentId], course.[TeacherId], COALESCE(schedule.[YearLevel], 1), course.[Capacity],
                    @AcademicYear, @Semester, CASE WHEN course.[IsActive] = 1 THEN N'Active' ELSE N'Paused' END, course.[CreatedAtUtc], course.[UpdatedAtUtc]
                FROM [Courses] course
                OUTER APPLY (SELECT MIN(entry.[YearLevel]) AS [YearLevel] FROM [ScheduleEntries] entry WHERE entry.[CourseId] = course.[Id] AND entry.[Status] <> N'Cancelled') schedule;

                INSERT INTO [Enrollment].[ClassroomAssignments]
                    ([Id], [ClassroomId], [DepartmentId], [Capacity], [Access], [AcademicYear], [Semester], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT NEWID(), [Id], [DepartmentId], [Capacity],
                    CASE WHEN [DepartmentId] IS NULL THEN N'Shared institute' ELSE N'Department only' END,
                    @AcademicYear, @Semester, CASE WHEN [Status] = N'Inactive' THEN N'Unavailable' ELSE N'Available' END, [CreatedAtUtc], [UpdatedAtUtc]
                FROM [Classrooms];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassroomAssignments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "CourseAssignments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "StudentEnrollments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "TeacherAssignments",
                schema: "Enrollment");
        }
    }
}
