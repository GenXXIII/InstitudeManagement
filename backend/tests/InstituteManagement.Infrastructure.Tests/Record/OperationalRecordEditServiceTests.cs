using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Record;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Record;

public sealed class OperationalRecordEditServiceTests
{
    [Fact]
    public async Task Current_session_can_be_corrected_but_becomes_read_only_after_rollover()
    {
        await using var db = CreateContext();
        var first = new SessionStudentSnapshot(Guid.NewGuid(), "STU-1", "Dara Sok", "Present", "07:30");
        var second = new SessionStudentSnapshot(Guid.NewGuid(), "STU-2", "Srey Mom", "Absent", "");
        var session = new ClassSessionRecord
        {
            AcademicYear = "2026\u20132027", Term = "Semester 1", CourseName = "English", YearLevel = 1,
            StudentCount = 2, PresentCount = 1, AbsentCount = 1, StudentAttendanceJson = JsonSerializer.Serialize(new[] { first, second })
        };
        db.AddRange(session, Setting("academic-year", "currentYear", "2026\u20132027"), Setting("semester", "currentTerm", "Semester 1"));
        await db.SaveChangesAsync();
        var service = new OperationalRecordEditService(db, new InstituteCache());

        await service.UpdateClassSessionAsync(session.Id, new UpdateClassSessionRecordDto([
            new(first.StudentId, "Late", "07:45"),
            new(second.StudentId, "Permission", "")
        ]), CancellationToken.None);

        Assert.Equal(0, session.PresentCount);
        Assert.Equal(1, session.LateCount);
        Assert.Equal(0, session.AbsentCount);
        Assert.Equal(1, session.ExcusedCount);
        Assert.Contains(db.AuditLogs, log => log.ResourceId == session.Id && log.Action == "Attendance corrected");

        db.SystemSettings.Single(setting => setting.Section == "semester" && setting.Key == "currentTerm").Value = "Semester 2";
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateClassSessionAsync(session.Id, new UpdateClassSessionRecordDto([
            new(first.StudentId, "Present", "07:30"), new(second.StudentId, "Absent", "")
        ]), CancellationToken.None));
        Assert.Contains("read-only Record History", error.Message);
    }

    private static SystemSetting Setting(string section, string key, string value) => new() { Section = section, Key = key, Value = value };
    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
