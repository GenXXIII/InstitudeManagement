using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Persistence;

public sealed class InstituteDbContextModelTests
{
    [Fact]
    public void Model_has_unique_identifiers_and_restrictive_relationships()
    {
        using var db = CreateContext();

        AssertUniqueIndex<Student>(db, nameof(Student.StudentCode));
        AssertUniqueIndex<Teacher>(db, nameof(Teacher.TeacherCode));
        AssertUniqueIndex<Department>(db, nameof(Department.DepartmentCode));
        AssertUniqueIndex<Course>(db, nameof(Course.CourseCode));
        AssertUniqueIndex<Classroom>(db, nameof(Classroom.ClassroomCode));
        AssertUniqueIndex<ScheduleEntry>(db, nameof(ScheduleEntry.TimetableCode));
        AssertUniqueIndex<AttendanceRecord>(db, nameof(AttendanceRecord.AttendanceCode));
        AssertUniqueIndex<GradeRecord>(db, nameof(GradeRecord.GradeCode));
        AssertUniqueIndex<AuditLog>(db, nameof(AuditLog.AuditLogCode));
        AssertUniqueIndex<ClassSessionRecord>(db, nameof(ClassSessionRecord.ClassSessionRecordCode));
        AssertUniqueIndex<Notification>(db, nameof(Notification.NotificationCode));
        AssertUniqueIndex<SystemSetting>(db, nameof(SystemSetting.SystemSettingCode));
        AssertUniqueIndex<Announcement>(db, nameof(Announcement.AnnouncementCode));
        AssertUniqueIndex<NotificationHistory>(db, nameof(NotificationHistory.NotificationHistoryCode));
        AssertUniqueIndex<AttendanceRecord>(db, nameof(AttendanceRecord.StudentId), nameof(AttendanceRecord.Date));
        AssertUniqueIndex<GradeRecord>(db, nameof(GradeRecord.StudentId), nameof(GradeRecord.CourseId), nameof(GradeRecord.AcademicYear), nameof(GradeRecord.Term));
        AssertUniqueIndex<ClassSessionRecord>(db, nameof(ClassSessionRecord.ScheduleEntryId), nameof(ClassSessionRecord.SessionDate));
        Assert.Equal(1, db.Model.FindEntityType(typeof(ScheduleEntry))?.FindProperty(nameof(ScheduleEntry.YearLevel))?.GetDefaultValue());
        Assert.All(
            db.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static void AssertUniqueIndex<TEntity>(InstituteDbContext db, params string[] properties)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(properties));
    }

    private static InstituteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InstituteDbContext(options);
    }
}
