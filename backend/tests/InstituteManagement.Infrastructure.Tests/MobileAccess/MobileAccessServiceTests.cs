using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Management.Students;
using InstituteManagement.Infrastructure.Services.MobileAccess;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.MobileAccess;

public sealed class MobileAccessServiceTests
{
    [Fact]
    public async Task Creating_student_assigns_public_id_automatically()
    {
        await using var db = CreateContext();
        var values = new Dictionary<string, string>
        {
            ["studentCode"] = "1",
            ["name"] = "Student One",
            ["email"] = "student@example.com",
            ["photoDataUrl"] = "data:image/png;base64,AA==",
        };

        var created = await new StudentManagementService(db, new InstituteCache()).CreateAsync(values, CancellationToken.None);

        Assert.StartsWith("STU-", created.Values.PublicId);
        Assert.Equal(created.Values.PublicId, (await db.Students.SingleAsync()).PublicId);
    }

    [Fact]
    public async Task SignIn_resolves_active_profile_by_generated_public_id()
    {
        await using var db = CreateContext();
        var student = new Student { StudentCode = "STU-1", FullName = "Student One" };
        student.PublicId = PublicAccessId.ForStudent(student.Id);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var session = await new MobileAccessService(db).SignInAsync(student.PublicId.ToLowerInvariant(), "1234", CancellationToken.None);

        Assert.NotNull(session);
        Assert.Equal("student", session.Role);
        Assert.Equal(student.PublicId, session.PublicId);
        Assert.Equal(student.Id, session.ProfileId);
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("")]
    public async Task SignIn_rejects_an_incorrect_password(string password)
    {
        await using var db = CreateContext();
        var teacher = new Teacher { TeacherCode = "TEA-1", FullName = "Teacher One" };
        teacher.PublicId = PublicAccessId.ForTeacher(teacher.Id);
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();

        var session = await new MobileAccessService(db).SignInAsync(teacher.PublicId, password, CancellationToken.None);

        Assert.Null(session);
    }

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
