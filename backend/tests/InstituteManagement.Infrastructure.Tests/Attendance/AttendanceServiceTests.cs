using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Attendance;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Attendance;

public sealed class AttendanceServiceTests
{
    [Fact]
    public async Task RecordAsync_updates_the_existing_daily_record()
    {
        await using var db = CreateContext();
        var department = new Department { Code = "IT", Name = "Information Technology" };
        var student = new Student
        {
            StudentNumber = "ST-001",
            FullName = "Sok Dara",
            Email = "sok@example.edu",
            DepartmentId = department.Id,
            Status = "Active"
        };
        db.AddRange(department, student);
        await db.SaveChangesAsync();
        var service = new AttendanceService(db, new InstituteCache());

        await service.RecordAsync(student.Id, "Present", CancellationToken.None);
        await service.RecordAsync(student.Id, "Late", CancellationToken.None);

        var record = Assert.Single(await db.AttendanceRecords.ToListAsync());
        Assert.Equal("Late", record.Status);
        Assert.Equal(2, await db.AuditLogs.CountAsync());
    }

    private static InstituteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InstituteDbContext(options);
    }
}
