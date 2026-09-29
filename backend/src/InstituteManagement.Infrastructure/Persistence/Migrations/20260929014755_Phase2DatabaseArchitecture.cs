using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstituteManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2DatabaseArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @financialEnrollmentForeignKey sysname;
                SELECT TOP (1) @financialEnrollmentForeignKey = foreignKey.[name]
                FROM sys.foreign_keys foreignKey
                INNER JOIN sys.foreign_key_columns foreignKeyColumn
                    ON foreignKeyColumn.[constraint_object_id] = foreignKey.[object_id]
                INNER JOIN sys.columns parentColumn
                    ON parentColumn.[object_id] = foreignKeyColumn.[parent_object_id]
                    AND parentColumn.[column_id] = foreignKeyColumn.[parent_column_id]
                WHERE foreignKey.[parent_object_id] = OBJECT_ID(N'[Finance].[FinancialAccounts]')
                    AND parentColumn.[name] = N'StudentEnrollmentId';

                IF @financialEnrollmentForeignKey IS NOT NULL
                BEGIN
                    DECLARE @dropFinancialEnrollmentForeignKeySql nvarchar(max) =
                        N'ALTER TABLE [Finance].[FinancialAccounts] DROP CONSTRAINT '
                        + QUOTENAME(@financialEnrollmentForeignKey) + N';';
                    EXEC sys.sp_executesql @dropFinancialEnrollmentForeignKeySql;
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Departments_DepartmentId",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_CourseId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                table: "AttendanceRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentEnrollments",
                schema: "Enrollment",
                table: "StudentEnrollments");

            migrationBuilder.EnsureSchema(
                name: "Record");

            migrationBuilder.RenameTable(
                name: "StudentEnrollments",
                schema: "Enrollment",
                newName: "EnrollmentSemesters",
                newSchema: "Enrollment");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_StudentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_StudentId_AcademicYear_Semester");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_ResultCode",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_ResultCode");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_PublicId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_FinanceCode",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_FinanceCode");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_EnrollmentCode_AcademicYear_Semester");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_DepartmentId_YearLevel");

            migrationBuilder.RenameIndex(
                name: "IX_StudentEnrollments_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                newName: "IX_EnrollmentSemesters_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentEnrollmentId",
                schema: "Grades",
                table: "SemesterResultPublications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Finance",
                table: "Payments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GradeRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentEnrollmentId",
                table: "GradeRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Finance",
                table: "FinancialAccounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttendanceRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentEnrollmentId",
                table: "AttendanceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentAcademicEnrollmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_EnrollmentSemesters_Id_StudentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                columns: new[] { "Id", "StudentId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_EnrollmentSemesters",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ClassSessionStudentAttendance",
                schema: "Record",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassSessionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CheckedInAt = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessionStudentAttendance", x => x.Id);
                    table.CheckConstraint("CK_ClassSessionStudentAttendance_Status", "[Status] IN (N'Present', N'Late', N'Absent', N'Excused', N'Permission', N'Class not held', N'Not recorded')");
                    table.ForeignKey(
                        name: "FK_ClassSessionStudentAttendance_ClassSessionRecords_ClassSessionRecordId",
                        column: x => x.ClassSessionRecordId,
                        principalTable: "ClassSessionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessionStudentAttendance_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                        columns: x => new { x.StudentEnrollmentId, x.StudentId },
                        principalSchema: "Enrollment",
                        principalTable: "EnrollmentSemesters",
                        principalColumns: new[] { "Id", "StudentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessionStudentAttendance_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAcademicEnrollments",
                schema: "Enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAcademicEnrollments", x => x.Id);
                    table.CheckConstraint("CK_StudentAcademicEnrollments_Completion", "([Status] = N'Active' AND [CompletedAtUtc] IS NULL) OR ([Status] <> N'Active' AND [CompletedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_StudentAcademicEnrollments_Status", "[Status] IN (N'Active', N'Completed', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_StudentAcademicEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT [EnrollmentCode]
                    FROM [Enrollment].[EnrollmentSemesters]
                    GROUP BY [EnrollmentCode]
                    HAVING COUNT(DISTINCT [StudentId]) > 1
                )
                    THROW 51000, 'Phase 2 migration stopped: an EnrollmentCode is owned by more than one Student.', 1;

                INSERT INTO [Enrollment].[StudentAcademicEnrollments]
                    ([Id], [EnrollmentCode], [StudentId], [Status], [StartedAtUtc], [CompletedAtUtc], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    [EnrollmentCode],
                    [StudentId],
                    CASE
                        WHEN MAX(CASE WHEN [Status] IN (N'Active', N'Paused') THEN 1 ELSE 0 END) = 1 THEN N'Active'
                        WHEN MAX(CASE WHEN [Status] = N'Removed' THEN 1 ELSE 0 END) = 1 THEN N'Cancelled'
                        ELSE N'Completed'
                    END,
                    MIN([CreatedAtUtc]),
                    CASE
                        WHEN MAX(CASE WHEN [Status] IN (N'Active', N'Paused') THEN 1 ELSE 0 END) = 1 THEN NULL
                        ELSE MAX([UpdatedAtUtc])
                    END,
                    MIN([CreatedAtUtc]),
                    MAX([UpdatedAtUtc])
                FROM [Enrollment].[EnrollmentSemesters]
                GROUP BY [EnrollmentCode], [StudentId];

                UPDATE semester
                SET [StudentAcademicEnrollmentId] = enrollment.[Id]
                FROM [Enrollment].[EnrollmentSemesters] semester
                INNER JOIN [Enrollment].[StudentAcademicEnrollments] enrollment
                    ON enrollment.[EnrollmentCode] = semester.[EnrollmentCode]
                    AND enrollment.[StudentId] = semester.[StudentId];

                UPDATE attendance
                SET [StudentEnrollmentId] = semester.[Id]
                FROM [AttendanceRecords] attendance
                INNER JOIN [Enrollment].[EnrollmentSemesters] semester
                    ON semester.[StudentId] = attendance.[StudentId]
                    AND semester.[AcademicYear] = attendance.[AcademicYear]
                    AND semester.[Semester] = attendance.[Term];

                UPDATE grade
                SET [StudentEnrollmentId] = semester.[Id]
                FROM [GradeRecords] grade
                INNER JOIN [Enrollment].[EnrollmentSemesters] semester
                    ON semester.[StudentId] = grade.[StudentId]
                    AND semester.[AcademicYear] = grade.[AcademicYear]
                    AND semester.[Semester] = grade.[Term];

                UPDATE publication
                SET [StudentEnrollmentId] = semester.[Id]
                FROM [Grades].[SemesterResultPublications] publication
                INNER JOIN [Enrollment].[EnrollmentSemesters] semester
                    ON semester.[StudentId] = publication.[StudentId]
                    AND semester.[AcademicYear] = publication.[AcademicYear]
                    AND semester.[Semester] = publication.[Term];

                IF EXISTS (SELECT 1 FROM [Enrollment].[EnrollmentSemesters] WHERE [StudentAcademicEnrollmentId] IS NULL)
                    THROW 51001, 'Phase 2 migration stopped: an Enrollment Semester could not be attached to its academic journey.', 1;
                IF EXISTS (SELECT 1 FROM [AttendanceRecords] WHERE [StudentEnrollmentId] IS NULL)
                    THROW 51002, 'Phase 2 migration stopped: an Attendance row has no matching Enrollment Semester.', 1;
                IF EXISTS (SELECT 1 FROM [GradeRecords] WHERE [StudentEnrollmentId] IS NULL)
                    THROW 51003, 'Phase 2 migration stopped: a Grade row has no matching Enrollment Semester.', 1;
                IF EXISTS (SELECT 1 FROM [Grades].[SemesterResultPublications] WHERE [StudentEnrollmentId] IS NULL)
                    THROW 51004, 'Phase 2 migration stopped: a Result publication has no matching Enrollment Semester.', 1;

                ;WITH [RankedTransactionReferences] AS (
                    SELECT
                        [Id],
                        [Status],
                        ROW_NUMBER() OVER (
                            PARTITION BY [TransactionReference]
                            ORDER BY
                                CASE WHEN [Status] = N'Completed' THEN 0 ELSE 1 END,
                                [PaidAtUtc],
                                [Id]
                        ) AS [ReferenceOccurrence]
                    FROM [Finance].[Payments]
                    WHERE [TransactionReference] <> N''
                )
                UPDATE payment
                SET [TransactionReference] =
                    LEFT(payment.[TransactionReference], 210)
                    + N' [legacy '
                    + CONVERT(nvarchar(36), payment.[Id])
                    + N']'
                FROM [Finance].[Payments] payment
                INNER JOIN [RankedTransactionReferences] ranked ON ranked.[Id] = payment.[Id]
                WHERE ranked.[ReferenceOccurrence] > 1
                    AND ranked.[Status] <> N'Completed';

                IF EXISTS (
                    SELECT [TransactionReference]
                    FROM [Finance].[Payments]
                    WHERE [TransactionReference] <> N''
                    GROUP BY [TransactionReference]
                    HAVING COUNT(*) > 1
                )
                    THROW 51005, 'Phase 2 migration stopped: duplicate completed Finance transaction references must be resolved first.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [ClassSessionRecords]
                    WHERE NULLIF(LTRIM(RTRIM([StudentAttendanceJson])), N'') IS NOT NULL
                        AND ISJSON([StudentAttendanceJson]) <> 1
                )
                    THROW 51007, 'Phase 2 migration stopped: class-session attendance contains malformed JSON.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [ClassSessionRecords] session
                    CROSS APPLY OPENJSON(
                        CASE WHEN ISJSON(session.[StudentAttendanceJson]) = 1
                            THEN session.[StudentAttendanceJson]
                            ELSE N'[]'
                        END
                    ) WITH (
                        [StudentId] uniqueidentifier '$.StudentId'
                    ) snapshot
                    LEFT JOIN [Enrollment].[EnrollmentSemesters] semester
                        ON semester.[StudentId] = snapshot.[StudentId]
                        AND semester.[AcademicYear] = session.[AcademicYear]
                        AND semester.[Semester] = session.[Term]
                    WHERE snapshot.[StudentId] IS NOT NULL
                        AND semester.[Id] IS NULL
                )
                    THROW 51006, 'Phase 2 migration stopped: class-session attendance has no matching Enrollment Semester.', 1;

                INSERT INTO [Record].[ClassSessionStudentAttendance]
                    ([Id], [ClassSessionRecordId], [StudentEnrollmentId], [StudentId], [StudentCode], [StudentName],
                     [Status], [CheckedInAt], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    session.[Id],
                    semester.[Id],
                    snapshot.[StudentId],
                    snapshot.[StudentCode],
                    snapshot.[StudentName],
                    snapshot.[Status],
                    snapshot.[CheckedInAt],
                    session.[CreatedAtUtc],
                    session.[UpdatedAtUtc]
                FROM [ClassSessionRecords] session
                CROSS APPLY OPENJSON(
                    CASE WHEN ISJSON(session.[StudentAttendanceJson]) = 1
                        THEN session.[StudentAttendanceJson]
                        ELSE N'[]'
                    END
                ) WITH (
                    [StudentId] uniqueidentifier '$.StudentId',
                    [StudentCode] nvarchar(32) '$.StudentCode',
                    [StudentName] nvarchar(200) '$.StudentName',
                    [Status] nvarchar(32) '$.Status',
                    [CheckedInAt] nvarchar(5) '$.CheckedInAt'
                ) snapshot
                INNER JOIN [Enrollment].[EnrollmentSemesters] semester
                    ON semester.[StudentId] = snapshot.[StudentId]
                    AND semester.[AcademicYear] = session.[AcademicYear]
                    AND semester.[Semester] = session.[Term];
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentAcademicEnrollmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentEnrollmentId",
                table: "AttendanceRecords",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentEnrollmentId",
                table: "GradeRecords",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentEnrollmentId",
                schema: "Grades",
                table: "SemesterResultPublications",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "StudentAttendanceJson",
                table: "ClassSessionRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_DepartmentId_TeacherCode",
                table: "Teachers",
                columns: new[] { "DepartmentId", "TeacherCode" })
                .Annotation("SqlServer:Include", new[] { "FullName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DepartmentId_StudentCode",
                table: "Students",
                columns: new[] { "DepartmentId", "StudentCode" })
                .Annotation("SqlServer:Include", new[] { "FullName", "YearLevel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_StudentEnrollmentId",
                schema: "Grades",
                table: "SemesterResultPublications",
                column: "StudentEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResultPublications_StudentEnrollmentId_StudentId",
                schema: "Grades",
                table: "SemesterResultPublications",
                columns: new[] { "StudentEnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionReference",
                schema: "Finance",
                table: "Payments",
                column: "TransactionReference",
                unique: true,
                filter: "[TransactionReference] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount",
                schema: "Finance",
                table: "Payments",
                sql: "[Amount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Status",
                schema: "Finance",
                table: "Payments",
                sql: "[Status] IN (N'Completed', N'Cancelled', N'Refunded')");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_CourseId_AcademicYear_Term_ReviewStatus",
                table: "GradeRecords",
                columns: new[] { "CourseId", "AcademicYear", "Term", "ReviewStatus" })
                .Annotation("SqlServer:Include", new[] { "StudentId", "Score", "FinalizedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_StudentEnrollmentId_CourseId",
                table: "GradeRecords",
                columns: new[] { "StudentEnrollmentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_StudentEnrollmentId_StudentId",
                table: "GradeRecords",
                columns: new[] { "StudentEnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_StudentId_AcademicYear_Term",
                table: "GradeRecords",
                columns: new[] { "StudentId", "AcademicYear", "Term" })
                .Annotation("SqlServer:Include", new[] { "CourseId", "Score", "LetterGrade", "ReviewStatus", "FinalizedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradeRecords_ReviewStatus",
                table: "GradeRecords",
                sql: "[ReviewStatus] IN (N'Pending', N'SubmissionRequested', N'SubmissionAuthorized', N'Submitted', N'Approved', N'Rejected', N'ResubmitRequested', N'ResubmitAuthorized')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradeRecords_Scores",
                table: "GradeRecords",
                sql: "[AttendanceScore] >= 0 AND [AttendanceMaximum] > 0 AND [AssignmentScore] >= 0 AND [AssignmentMaximum] > 0 AND [MidtermScore] >= 0 AND [MidtermMaximum] > 0 AND [FinalExamScore] >= 0 AND [FinalExamMaximum] > 0 AND [Score] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_StudentEnrollmentId_StudentId",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "StudentEnrollmentId", "StudentId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialAccounts_Amounts",
                schema: "Finance",
                table: "FinancialAccounts",
                sql: "[TuitionFee] >= 0 AND [OtherFee] >= 0 AND [LatePenaltyDays] >= 0 AND [LatePenaltyAmount] >= 0 AND ([DeclaredAmount] IS NULL OR [DeclaredAmount] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialAccounts_Closure",
                schema: "Finance",
                table: "FinancialAccounts",
                sql: "[ClosedAtUtc] IS NULL OR [Status] = N'Paid'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialAccounts_Status",
                schema: "Finance",
                table: "FinancialAccounts",
                sql: "[Status] IN (N'Pending', N'Partial', N'Paid', N'Refunded', N'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId_YearLevel_Semester_CourseCode",
                table: "Courses",
                columns: new[] { "DepartmentId", "YearLevel", "Semester", "CourseCode" })
                .Annotation("SqlServer:Include", new[] { "Name", "IsActive", "TeacherId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentEnrollmentId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentEnrollmentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentEnrollmentId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "StudentEnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_AcademicYear_Term_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "AcademicYear", "Term", "Date" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AttendanceRecords_Status",
                table: "AttendanceRecords",
                sql: "[Status] IN (N'Present', N'Late', N'Absent', N'Excused', N'Permission')");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSemesters_StudentAcademicEnrollmentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                columns: new[] { "StudentAcademicEnrollmentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EnrollmentSemesters_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                sql: "[Semester] IN (N'Semester 1', N'Semester 2', N'Summer Term')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EnrollmentSemesters_Status",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                sql: "[Status] IN (N'Active', N'Paused', N'Completed', N'Removed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EnrollmentSemesters_YearLevel",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                sql: "[YearLevel] BETWEEN 1 AND 4");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStudentAttendance_ClassSessionRecordId_StudentId",
                schema: "Record",
                table: "ClassSessionStudentAttendance",
                columns: new[] { "ClassSessionRecordId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStudentAttendance_StudentEnrollmentId_ClassSessionRecordId",
                schema: "Record",
                table: "ClassSessionStudentAttendance",
                columns: new[] { "StudentEnrollmentId", "ClassSessionRecordId" })
                .Annotation("SqlServer:Include", new[] { "StudentId", "Status", "CheckedInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStudentAttendance_StudentEnrollmentId_StudentId",
                schema: "Record",
                table: "ClassSessionStudentAttendance",
                columns: new[] { "StudentEnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessionStudentAttendance_StudentId_ClassSessionRecordId",
                schema: "Record",
                table: "ClassSessionStudentAttendance",
                columns: new[] { "StudentId", "ClassSessionRecordId" })
                .Annotation("SqlServer:Include", new[] { "Status", "CheckedInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicEnrollments_EnrollmentCode",
                schema: "Enrollment",
                table: "StudentAcademicEnrollments",
                column: "EnrollmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicEnrollments_StudentId_Status",
                schema: "Enrollment",
                table: "StudentAcademicEnrollments",
                columns: new[] { "StudentId", "Status" })
                .Annotation("SqlServer:Include", new[] { "EnrollmentCode", "StartedAtUtc", "CompletedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "StudentEnrollmentId", "StudentId" },
                principalSchema: "Enrollment",
                principalTable: "EnrollmentSemesters",
                principalColumns: new[] { "Id", "StudentId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentSemesters_Departments_DepartmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentSemesters_StudentAcademicEnrollments_StudentAcademicEnrollmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                column: "StudentAcademicEnrollmentId",
                principalSchema: "Enrollment",
                principalTable: "StudentAcademicEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentSemesters_Students_StudentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialAccounts_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                schema: "Finance",
                table: "FinancialAccounts",
                columns: new[] { "StudentEnrollmentId", "StudentId" },
                principalSchema: "Enrollment",
                principalTable: "EnrollmentSemesters",
                principalColumns: new[] { "Id", "StudentId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                table: "GradeRecords",
                columns: new[] { "StudentEnrollmentId", "StudentId" },
                principalSchema: "Enrollment",
                principalTable: "EnrollmentSemesters",
                principalColumns: new[] { "Id", "StudentId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SemesterResultPublications_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                schema: "Grades",
                table: "SemesterResultPublications",
                columns: new[] { "StudentEnrollmentId", "StudentId" },
                principalSchema: "Enrollment",
                principalTable: "EnrollmentSemesters",
                principalColumns: new[] { "Id", "StudentId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentSemesters_Departments_DepartmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentSemesters_StudentAcademicEnrollments_StudentAcademicEnrollmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentSemesters_Students_StudentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialAccounts_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                table: "GradeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_SemesterResultPublications_EnrollmentSemesters_StudentEnrollmentId_StudentId",
                schema: "Grades",
                table: "SemesterResultPublications");

            migrationBuilder.AddColumn<string>(
                name: "StudentAttendanceJson",
                table: "ClassSessionRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(
                """
                UPDATE session
                SET [StudentAttendanceJson] = (
                    SELECT
                        attendance.[StudentId],
                        attendance.[StudentCode],
                        attendance.[StudentName],
                        attendance.[Status],
                        attendance.[CheckedInAt]
                    FROM [Record].[ClassSessionStudentAttendance] attendance
                    WHERE attendance.[ClassSessionRecordId] = session.[Id]
                    ORDER BY attendance.[StudentName], attendance.[StudentId]
                    FOR JSON PATH
                )
                FROM [ClassSessionRecords] session;
                """);

            migrationBuilder.DropTable(
                name: "ClassSessionStudentAttendance",
                schema: "Record");

            migrationBuilder.DropTable(
                name: "StudentAcademicEnrollments",
                schema: "Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_DepartmentId_TeacherCode",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Students_DepartmentId_StudentCode",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_SemesterResultPublications_StudentEnrollmentId",
                schema: "Grades",
                table: "SemesterResultPublications");

            migrationBuilder.DropIndex(
                name: "IX_SemesterResultPublications_StudentEnrollmentId_StudentId",
                schema: "Grades",
                table: "SemesterResultPublications");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionReference",
                schema: "Finance",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount",
                schema: "Finance",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Status",
                schema: "Finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_CourseId_AcademicYear_Term_ReviewStatus",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_StudentEnrollmentId_CourseId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_StudentEnrollmentId_StudentId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_StudentId_AcademicYear_Term",
                table: "GradeRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GradeRecords_ReviewStatus",
                table: "GradeRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GradeRecords_Scores",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_StudentEnrollmentId_StudentId",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialAccounts_Amounts",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialAccounts_Closure",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialAccounts_Status",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DepartmentId_YearLevel_Semester_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentEnrollmentId_Date",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentEnrollmentId_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_AcademicYear_Term_Date",
                table: "AttendanceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AttendanceRecords_Status",
                table: "AttendanceRecords");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EnrollmentSemesters_Id_StudentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EnrollmentSemesters",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentSemesters_StudentAcademicEnrollmentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EnrollmentSemesters_Semester",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EnrollmentSemesters_Status",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EnrollmentSemesters_YearLevel",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropColumn(
                name: "StudentEnrollmentId",
                schema: "Grades",
                table: "SemesterResultPublications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "StudentEnrollmentId",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Finance",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "StudentEnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.DropColumn(
                name: "StudentAcademicEnrollmentId",
                schema: "Enrollment",
                table: "EnrollmentSemesters");

            migrationBuilder.RenameTable(
                name: "EnrollmentSemesters",
                schema: "Enrollment",
                newName: "StudentEnrollments",
                newSchema: "Enrollment");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_StudentId_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_StudentId_AcademicYear_Semester");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_ResultCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_ResultCode");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_PublicId",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_FinanceCode",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_FinanceCode");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_EnrollmentCode_AcademicYear_Semester",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_EnrollmentCode_AcademicYear_Semester");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_DepartmentId_YearLevel");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentSemesters_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel",
                schema: "Enrollment",
                table: "StudentEnrollments",
                newName: "IX_StudentEnrollments_AcademicYear_Semester_Status_Shift_DepartmentId_YearLevel");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentEnrollments",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_CourseId",
                table: "GradeRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialAccounts_StudentEnrollments_StudentEnrollmentId",
                schema: "Finance",
                table: "FinancialAccounts",
                column: "StudentEnrollmentId",
                principalSchema: "Enrollment",
                principalTable: "StudentEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Departments_DepartmentId",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                schema: "Enrollment",
                table: "StudentEnrollments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
