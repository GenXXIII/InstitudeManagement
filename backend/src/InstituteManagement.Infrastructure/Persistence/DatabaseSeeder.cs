using InstituteManagement.Infrastructure.Persistence.SeedData;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            if (await HasExistingInstituteSchemaAsync(db, cancellationToken))
            {
                await DatabaseSchemaUpdater.EnsureAsync(db, cancellationToken);
                await MarkInitialMigrationAppliedAsync(db, cancellationToken);
            }
            await db.Database.MigrateAsync(cancellationToken);
        }
        else await db.Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSchemaUpdater.EnsureAsync(db, cancellationToken);
        if (await db.Departments.AnyAsync(cancellationToken))
        {
            await CurrentDataBackfill.ApplyAsync(db, cancellationToken);
            return;
        }

        var departments = DepartmentSeedFactory.Create();
        var teachers = TeacherSeedFactory.Create(departments);
        var rooms = ClassroomSeedFactory.Create(departments);
        var courses = CourseSeedFactory.Create(departments, teachers);
        var students = StudentSeedFactory.Create(departments);

        db.Departments.AddRange(departments);
        db.Teachers.AddRange(teachers);
        db.Classrooms.AddRange(rooms);
        db.Courses.AddRange(courses);
        db.Students.AddRange(students);
        const string academicYear = "2026\u20132027";
        const string term = "Semester 1";
        db.AttendanceRecords.AddRange(AttendanceSeedFactory.Create(students, academicYear, term));
        db.GradeRecords.AddRange(GradeSeedFactory.Create(students, courses, academicYear, term));
        db.ScheduleEntries.AddRange(TimetableSeedFactory.Create(courses, rooms));
        db.Notifications.AddRange(NotificationSeedFactory.Create());
        db.AuditLogs.AddRange(AuditLogSeedFactory.Create(students, courses, rooms));
        db.SystemSettings.AddRange(SystemSettingSeedFactory.Create());
        await db.SaveChangesAsync(cancellationToken);

        foreach (var department in departments)
        {
            var head = teachers.First(x => x.DepartmentId == department.Id);
            department.HeadTeacherId = head.Id;
            department.Head = head.FullName;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> HasExistingInstituteSchemaAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[Departments]', N'U') IS NULL THEN 0 ELSE 1 END";
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private static Task MarkInitialMigrationAppliedAsync(InstituteDbContext db, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;
            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260822023337_InitialInstituteSchema')
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260822023337_InitialInstituteSchema', N'10.0.11');
            """, cancellationToken);
}
