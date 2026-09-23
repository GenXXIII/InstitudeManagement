using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Main : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Attendance");

            migrationBuilder.EnsureSchema(
                name: "Enrollment");

            migrationBuilder.EnsureSchema(
                name: "Finance");

            migrationBuilder.EnsureSchema(
                name: "Grades");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditLogCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationHistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemSettingCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnouncementCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckedInAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassPermissionRequests",
                schema: "Attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPermissionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Building = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeviceOnline = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassSessionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassSessionRecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TeacherName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TeacherAttendanceStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClassroomCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    PresentCount = table.Column<int>(type: "int", nullable: false),
                    LateCount = table.Column<int>(type: "int", nullable: false),
                    AbsentCount = table.Column<int>(type: "int", nullable: false),
                    ExcusedCount = table.Column<int>(type: "int", nullable: false),
                    StudentAttendanceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSessionRecords_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Semester 1"),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Head = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeadTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PhotoDataUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    YearLevel = table.Column<int>(type: "int", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TeacherCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PhotoDataUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SemesterResultPublications",
                schema: "Grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterResultPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemesterResultPublications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FinanceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                name: "GradeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AttendanceMaximum = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AssignmentScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AssignmentMaximum = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MidtermScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MidtermMaximum = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalExamScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalExamMaximum = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LetterGrade = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubmittedByTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubmissionVersion = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeRecords_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeRecords_Teachers_SubmittedByTeacherId",
                        column: x => x.SubmittedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimetableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    YearLevel = table.Column<int>(type: "int", nullable: true),
                    Shift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Morning"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "Finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialAccountCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PaymentPlan = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DeclaredAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeclaredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BakongQrPayload = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    BakongMd5 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QrGeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QrExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TuitionFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LatePenaltyDays = table.Column<int>(type: "int", nullable: false),
                    LatePenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReminderSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalSchema: "Enrollment",
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimetableEnrollments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HistoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                        name: "FK_TimetableEnrollments_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableEnrollments_ScheduleEntries_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "ScheduleEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableEnrollments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "Finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "Finance",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AnnouncementCode",
                table: "Announcements",
                column: "AnnouncementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsActive_CreatedAtUtc",
                table: "Announcements",
                columns: new[] { "IsActive", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_NotificationId",
                table: "Announcements",
                column: "NotificationId",
                unique: true,
                filter: "[NotificationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AcademicYear_Term_CreatedAtUtc",
                table: "AttendanceRecords",
                columns: new[] { "AcademicYear", "Term", "CreatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceCode",
                table: "AttendanceRecords",
                column: "AttendanceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Date",
                table: "AttendanceRecords",
                column: "Date")
                .Annotation("SqlServer:Include", new[] { "StudentId", "Status", "CheckedInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditLogCode",
                table: "AuditLogs",
                column: "AuditLogCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc",
                descending: new bool[0])
                .Annotation("SqlServer:Include", new[] { "Action", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceId",
                table: "AuditLogs",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Type_Action_ResourceId",
                table: "AuditLogs",
                columns: new[] { "Type", "Action", "ResourceId" },
                filter: "[ResourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Type_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "Type", "CreatedAtUtc" });

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

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId" })
                .Annotation("SqlServer:Include", new[] { "ClassroomId", "EnrollmentCode" });

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
                name: "IX_ClassroomAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_ClassroomCode",
                table: "Classrooms",
                column: "ClassroomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_DepartmentId",
                table: "Classrooms",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_AcademicYear_Term",
                table: "ClassSessionRecords",
                columns: new[] { "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_ClassroomId",
                table: "ClassSessionRecords",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_ClassSessionRecordCode",
                table: "ClassSessionRecords",
                column: "ClassSessionRecordCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_CourseId",
                table: "ClassSessionRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_DepartmentId_YearLevel",
                table: "ClassSessionRecords",
                columns: new[] { "DepartmentId", "YearLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_ScheduleEntryId_SessionDate",
                table: "ClassSessionRecords",
                columns: new[] { "ScheduleEntryId", "SessionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_SessionDate",
                table: "ClassSessionRecords",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionRecords_TeacherId",
                table: "ClassSessionRecords",
                column: "TeacherId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_AcademicYear_Semester_Status_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId", "YearLevel" })
                .Annotation("SqlServer:Include", new[] { "CourseId", "TeacherId", "EnrollmentCode" });

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
                name: "IX_CourseAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "CourseAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_TeacherId",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId",
                table: "Courses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_YearLevel_Semester",
                table: "Courses",
                columns: new[] { "YearLevel", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadTeacherId",
                table: "Departments",
                column: "HeadTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AcademicYear_Semester_Status_DueOn",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "AcademicYear", "Semester", "Status", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_FinancialAccountCode_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "FinancialAccountCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_StudentEnrollmentId",
                schema: "Finance",
                table: "FinancialAccounts",
                column: "StudentEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_StudentId_AcademicYear_Semester",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_CourseId",
                table: "GradeRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_GradeCode",
                table: "GradeRecords",
                column: "GradeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_ReviewStatus_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "ReviewStatus", "AcademicYear", "Term" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_StudentId_CourseId_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "StudentId", "CourseId", "AcademicYear", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_SubmittedByTeacherId",
                table: "GradeRecords",
                column: "SubmittedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_UpdatedAtUtc",
                table: "GradeRecords",
                column: "UpdatedAtUtc")
                .Annotation("SqlServer:Include", new[] { "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_Kind_CreatedAtUtc",
                table: "NotificationHistory",
                columns: new[] { "Kind", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_NotificationHistoryCode",
                table: "NotificationHistory",
                column: "NotificationHistoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_SourceId",
                table: "NotificationHistory",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationCode",
                table: "Notifications",
                column: "NotificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FinancialAccountId_Status_PaidAtUtc",
                schema: "Finance",
                table: "Payments",
                columns: new[] { "FinancialAccountId", "Status", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentCode",
                schema: "Finance",
                table: "Payments",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ClassroomId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries",
                columns: new[] { "ClassroomId", "DayOfWeek", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_CourseId",
                table: "ScheduleEntries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries",
                columns: new[] { "DayOfWeek", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TeacherId_DayOfWeek_StartsAt_EndsAt",
                table: "ScheduleEntries",
                columns: new[] { "TeacherId", "DayOfWeek", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TimetableCode",
                table: "ScheduleEntries",
                column: "TimetableCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_PublishedAtUtc",
                schema: "Grades",
                table: "SemesterResultPublications",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_StudentId_AcademicYear_Term",
                schema: "Grades",
                table: "SemesterResultPublications",
                columns: new[] { "StudentId", "AcademicYear", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "AcademicYear", "Semester", "Status", "Shift", "DepartmentId", "YearLevel" })
                .Annotation("SqlServer:Include", new[] { "StudentId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "DepartmentId", "YearLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_DepartmentId",
                table: "Students",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_PublicId",
                table: "Students",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Section_Key",
                table: "SystemSettings",
                columns: new[] { "Section", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SystemSettingCode",
                table: "SystemSettings",
                column: "SystemSettingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_AcademicYear_Semester_Status_DepartmentId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "AcademicYear", "Semester", "Status", "DepartmentId" })
                .Annotation("SqlServer:Include", new[] { "TeacherId", "EnrollmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_DepartmentId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_PublicId",
                schema: "Enrollment",
                table: "TeacherAssignments",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_DepartmentId",
                table: "Teachers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_PublicId",
                table: "Teachers",
                column: "PublicId",
                unique: true,
                filter: "[PublicId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherCode",
                table: "Teachers",
                column: "TeacherCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_AcademicYear_Semester_Status",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "AcademicYear", "Semester", "Status" })
                .Annotation("SqlServer:Include", new[] { "ScheduleEntryId", "EnrollmentCode" });

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
                name: "IX_TimetableEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "EnrollmentCode", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_ScheduleEntryId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                columns: new[] { "ScheduleEntryId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEnrollments_TeacherId",
                schema: "Enrollment",
                table: "TimetableEnrollments",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId",
                table: "AttendanceRecords",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassPermissionRequests_Students_StudentId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassPermissionRequests_Teachers_TeacherId",
                schema: "Attendance",
                table: "ClassPermissionRequests",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomAssignments_Classrooms_ClassroomId",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomAssignments_Departments_DepartmentId",
                schema: "Enrollment",
                table: "ClassroomAssignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classrooms_Departments_DepartmentId",
                table: "Classrooms",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessionRecords_Courses_CourseId",
                table: "ClassSessionRecords",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessionRecords_ScheduleEntries_ScheduleEntryId",
                table: "ClassSessionRecords",
                column: "ScheduleEntryId",
                principalTable: "ScheduleEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessionRecords_Teachers_TeacherId",
                table: "ClassSessionRecords",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessionStarts_ScheduleEntries_ScheduleEntryId",
                schema: "Attendance",
                table: "ClassSessionStarts",
                column: "ScheduleEntryId",
                principalTable: "ScheduleEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessionStarts_Teachers_TeacherId",
                schema: "Attendance",
                table: "ClassSessionStarts",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseAssignments_Courses_CourseId",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseAssignments_Departments_DepartmentId",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseAssignments_Teachers_TeacherId",
                schema: "Enrollment",
                table: "CourseAssignments",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Teachers_TeacherId",
                table: "Courses",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Teachers_HeadTeacherId",
                table: "Departments",
                column: "HeadTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Teachers_HeadTeacherId",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ClassPermissionRequests",
                schema: "Attendance");

            migrationBuilder.DropTable(
                name: "ClassroomAssignments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "ClassSessionRecords");

            migrationBuilder.DropTable(
                name: "ClassSessionStarts",
                schema: "Attendance");

            migrationBuilder.DropTable(
                name: "CourseAssignments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "GradeRecords");

            migrationBuilder.DropTable(
                name: "NotificationHistory");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "Finance");

            migrationBuilder.DropTable(
                name: "SemesterResultPublications",
                schema: "Grades");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TeacherAssignments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "TimetableEnrollments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "Finance");

            migrationBuilder.DropTable(
                name: "ScheduleEntries");

            migrationBuilder.DropTable(
                name: "StudentEnrollments",
                schema: "Enrollment");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
