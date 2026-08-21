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

        AssertUniqueIndex<Student>(db, nameof(Student.StudentNumber));
        AssertUniqueIndex<Teacher>(db, nameof(Teacher.TeacherNumber));
        AssertUniqueIndex<Course>(db, nameof(Course.Code));
        AssertUniqueIndex<Classroom>(db, nameof(Classroom.Code));
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
