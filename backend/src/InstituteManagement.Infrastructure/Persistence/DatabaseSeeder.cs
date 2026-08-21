using InstituteManagement.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
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
}
