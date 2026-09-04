using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSchemaUpdater
{
    public static async Task EnsureAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational()) return;
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Students', 'PhotoDataUrl') IS NULL ALTER TABLE [Students] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Students_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Teachers', 'PhotoDataUrl') IS NULL ALTER TABLE [Teachers] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Teachers_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Departments', 'HeadTeacherId') IS NULL ALTER TABLE [Departments] ADD [HeadTeacherId] uniqueidentifier NULL;
            IF COL_LENGTH('Classrooms', 'DepartmentId') IS NULL ALTER TABLE [Classrooms] ADD [DepartmentId] uniqueidentifier NULL;
            IF COL_LENGTH('Classrooms', 'RoomType') IS NULL ALTER TABLE [Classrooms] ADD [RoomType] nvarchar(32) NOT NULL CONSTRAINT [DF_Classrooms_RoomType] DEFAULT 'Classroom';
            IF COL_LENGTH('ScheduleEntries', 'YearLevel') IS NULL ALTER TABLE [ScheduleEntries] ADD [YearLevel] int NOT NULL CONSTRAINT [DF_ScheduleEntries_YearLevel] DEFAULT 1;
            IF COL_LENGTH('AuditLogs', 'ResourceId') IS NULL ALTER TABLE [AuditLogs] ADD [ResourceId] uniqueidentifier NULL;
            IF COL_LENGTH('AttendanceRecords', 'AcademicYear') IS NULL ALTER TABLE [AttendanceRecords] ADD [AcademicYear] nvarchar(32) NOT NULL CONSTRAINT [DF_AttendanceRecords_AcademicYear] DEFAULT '';
            IF COL_LENGTH('AttendanceRecords', 'Term') IS NULL ALTER TABLE [AttendanceRecords] ADD [Term] nvarchar(32) NOT NULL CONSTRAINT [DF_AttendanceRecords_Term] DEFAULT '';
            IF COL_LENGTH('GradeRecords', 'AcademicYear') IS NULL ALTER TABLE [GradeRecords] ADD [AcademicYear] nvarchar(32) NOT NULL CONSTRAINT [DF_GradeRecords_AcademicYear] DEFAULT '';
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GradeRecords_StudentId_CourseId_Term' AND object_id = OBJECT_ID('GradeRecords')) DROP INDEX [IX_GradeRecords_StudentId_CourseId_Term] ON [GradeRecords];
            IF COL_LENGTH('GradeRecords', 'Term') = -1 ALTER TABLE [GradeRecords] ALTER COLUMN [Term] nvarchar(64) NOT NULL;
            IF OBJECT_ID(N'[ClassSessionRecords]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ClassSessionRecords] (
                    [Id] uniqueidentifier NOT NULL,
                    [ClassSessionRecordCode] nvarchar(64) NOT NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [UpdatedAtUtc] datetime2 NOT NULL,
                    [ScheduleEntryId] uniqueidentifier NOT NULL,
                    [SessionDate] date NOT NULL,
                    [AcademicYear] nvarchar(32) NOT NULL,
                    [Term] nvarchar(32) NOT NULL,
                    [DepartmentId] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [TeacherId] uniqueidentifier NOT NULL,
                    [ClassroomId] uniqueidentifier NOT NULL,
                    [YearLevel] int NOT NULL,
                    [StartsAt] time NOT NULL,
                    [EndsAt] time NOT NULL,
                    [CourseName] nvarchar(256) NOT NULL,
                    [TeacherName] nvarchar(256) NOT NULL,
                    [ClassroomCode] nvarchar(64) NOT NULL,
                    [StudentCount] int NOT NULL,
                    [PresentCount] int NOT NULL,
                    [LateCount] int NOT NULL,
                    [AbsentCount] int NOT NULL,
                    [ExcusedCount] int NOT NULL,
                    [StudentAttendanceJson] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_ClassSessionRecords] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ClassSessionRecords_ScheduleEntries_ScheduleEntryId] FOREIGN KEY ([ScheduleEntryId]) REFERENCES [ScheduleEntries] ([Id]),
                    CONSTRAINT [FK_ClassSessionRecords_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]),
                    CONSTRAINT [FK_ClassSessionRecords_Teachers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]),
                    CONSTRAINT [FK_ClassSessionRecords_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [Classrooms] ([Id])
                );
                CREATE UNIQUE INDEX [IX_ClassSessionRecords_ScheduleEntryId_SessionDate] ON [ClassSessionRecords] ([ScheduleEntryId], [SessionDate]);
                CREATE UNIQUE INDEX [IX_ClassSessionRecords_ClassSessionRecordCode] ON [ClassSessionRecords] ([ClassSessionRecordCode]);
                CREATE INDEX [IX_ClassSessionRecords_CourseId] ON [ClassSessionRecords] ([CourseId]);
                CREATE INDEX [IX_ClassSessionRecords_TeacherId] ON [ClassSessionRecords] ([TeacherId]);
                CREATE INDEX [IX_ClassSessionRecords_ClassroomId] ON [ClassSessionRecords] ([ClassroomId]);
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GradeRecords_StudentId_CourseId_AcademicYear_Term' AND object_id = OBJECT_ID('GradeRecords')) CREATE UNIQUE INDEX [IX_GradeRecords_StudentId_CourseId_AcademicYear_Term] ON [GradeRecords] ([StudentId], [CourseId], [AcademicYear], [Term]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_HeadTeacherId') CREATE INDEX [IX_Departments_HeadTeacherId] ON [Departments] ([HeadTeacherId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Classrooms_DepartmentId') CREATE INDEX [IX_Classrooms_DepartmentId] ON [Classrooms] ([DepartmentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_ResourceId') CREATE INDEX [IX_AuditLogs_ResourceId] ON [AuditLogs] ([ResourceId]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_Teachers_HeadTeacherId') ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Teachers_HeadTeacherId] FOREIGN KEY ([HeadTeacherId]) REFERENCES [Teachers] ([Id]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Classrooms_Departments_DepartmentId') ALTER TABLE [Classrooms] ADD CONSTRAINT [FK_Classrooms_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);

            UPDATE [Classrooms] SET [Status] = 'Available' WHERE [Status] = 'Running';
            UPDATE [Classrooms] SET [Status] = 'Maintenance' WHERE [Status] = 'Starting';
            UPDATE [Classrooms] SET [Status] = 'Maintenance' WHERE [Status] IN ('Offline', 'Unavailable');
            IF OBJECT_ID(N'[Enrollment].[ClassroomAssignments]', N'U') IS NOT NULL
                UPDATE [Enrollment].[ClassroomAssignments] SET [Status] = 'Maintenance' WHERE [Status] IN ('Reserved', 'Unavailable');

            IF OBJECT_ID(N'[Notifications]', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM [Notifications] WHERE LEN([NotificationCode]) = 36 AND [NotificationCode] LIKE 'NOT-%' AND SUBSTRING([NotificationCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
            BEGIN
                ;WITH [Numbered] AS (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                    FROM [Notifications]
                )
                UPDATE [item] SET [NotificationCode] = CONCAT('NOT-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
                FROM [Notifications] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
            END;

            IF OBJECT_ID(N'[Announcements]', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM [Announcements] WHERE LEN([AnnouncementCode]) = 36 AND [AnnouncementCode] LIKE 'ANN-%' AND SUBSTRING([AnnouncementCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
            BEGIN
                ;WITH [Numbered] AS (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                    FROM [Announcements]
                )
                UPDATE [item] SET [AnnouncementCode] = CONCAT('ANN-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
                FROM [Announcements] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
            END;

            IF OBJECT_ID(N'[NotificationHistory]', N'U') IS NOT NULL
            BEGIN
                UPDATE [history]
                SET [SourceCode] = COALESCE([notification].[NotificationCode], [announcement].[AnnouncementCode], [history].[SourceCode])
                FROM [NotificationHistory] [history]
                LEFT JOIN [Notifications] [notification] ON [history].[Kind] = 'Notification' AND [notification].[Id] = [history].[SourceId]
                LEFT JOIN [Announcements] [announcement] ON [history].[Kind] = 'Alert' AND [announcement].[Id] = [history].[SourceId];

                IF EXISTS (SELECT 1 FROM [NotificationHistory] WHERE LEN([NotificationHistoryCode]) = 36 AND [NotificationHistoryCode] LIKE 'NHS-%' AND SUBSTRING([NotificationHistoryCode], 5, 32) NOT LIKE '%[^0-9A-Fa-f]%')
                BEGIN
                    ;WITH [Numbered] AS (
                        SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                        FROM [NotificationHistory]
                    )
                    UPDATE [item] SET [NotificationHistoryCode] = CONCAT('NHS-', RIGHT(CONCAT('00000000', [numbered].[Sequence]), 8))
                    FROM [NotificationHistory] [item] INNER JOIN [Numbered] [numbered] ON [numbered].[Id] = [item].[Id];
                END;
            END;

            UPDATE [ScheduleEntries]
            SET [StartsAt] = CASE
                    WHEN [StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time) THEN CAST('07:30' AS time)
                    WHEN [StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time) THEN CAST('09:15' AS time)
                    WHEN [StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time) THEN CAST('11:00' AS time)
                    WHEN [StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time) THEN CAST('14:00' AS time)
                    WHEN [StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time) THEN CAST('15:30' AS time)
                    WHEN [StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time) THEN CAST('17:30' AS time)
                    ELSE [StartsAt]
                END,
                [EndsAt] = CASE
                    WHEN [StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time) THEN CAST('09:00' AS time)
                    WHEN [StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time) THEN CAST('10:45' AS time)
                    WHEN [StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time) THEN CAST('12:30' AS time)
                    WHEN [StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time) THEN CAST('15:30' AS time)
                    WHEN [StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time) THEN CAST('17:00' AS time)
                    WHEN [StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time) THEN CAST('19:00' AS time)
                    ELSE [EndsAt]
                END
            WHERE [DayOfWeek] BETWEEN 1 AND 5
              AND (([StartsAt] = CAST('08:00' AS time) AND [EndsAt] = CAST('09:00' AS time))
                OR ([StartsAt] = CAST('09:00' AS time) AND [EndsAt] = CAST('10:00' AS time))
                OR ([StartsAt] = CAST('10:00' AS time) AND [EndsAt] = CAST('11:00' AS time))
                OR ([StartsAt] = CAST('11:00' AS time) AND [EndsAt] = CAST('12:00' AS time))
                OR ([StartsAt] = CAST('12:00' AS time) AND [EndsAt] = CAST('13:00' AS time))
                OR ([StartsAt] = CAST('13:00' AS time) AND [EndsAt] = CAST('14:00' AS time)));
            """, cancellationToken);
    }
}
