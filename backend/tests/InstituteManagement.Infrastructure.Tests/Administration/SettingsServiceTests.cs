using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Administration;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task Saving_first_institute_configuration_creates_standard_and_custom_settings()
    {
        await using var db = CreateContext();
        var service = new SettingsService(db, new InstituteCache());
        var values = new Dictionary<string, string>
        {
            ["name"] = "Institude of New Khmer",
            ["shortName"] = "INK",
            ["email"] = "office@ink.edu.kh",
            ["phone"] = "+855 12 345 678",
            ["address"] = "Phnom Penh",
            ["accentColor"] = "royal blue"
        };

        var result = await service.SaveAsync("institute", values, CancellationToken.None);

        Assert.Equal(values, result.Values);
        Assert.Equal(6, await db.SystemSettings.CountAsync());
        Assert.Contains(db.AuditLogs, log => log.Type == "Settings" && log.Subject == "institute");
    }

    [Fact]
    public async Task Replacing_configuration_removes_omitted_custom_settings()
    {
        await using var db = CreateContext();
        db.SystemSettings.AddRange(Settings("courses", new() { ["defaultCapacity"] = "40", ["requireAssignedTeacher"] = "true", ["deliveryMode"] = "Hybrid" }));
        await db.SaveChangesAsync();
        var service = new SettingsService(db, new InstituteCache());

        await service.SaveAsync("courses", new() { ["defaultCapacity"] = "30", ["requireAssignedTeacher"] = "true" }, CancellationToken.None);

        Assert.DoesNotContain(db.SystemSettings, setting => setting.Key == "deliveryMode");
    }

    [Fact]
    public async Task Saving_grade_rules_recalculates_existing_E_and_F_letters()
    {
        await using var db = CreateContext();
        db.SystemSettings.AddRange(Settings("grade-rules", new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "60", ["eMinimum"] = "50" }));
        var eGrade = new GradeRecord { StudentId = Guid.NewGuid(), CourseId = Guid.NewGuid(), Score = 55, LetterGrade = "F" };
        var fGrade = new GradeRecord { StudentId = Guid.NewGuid(), CourseId = Guid.NewGuid(), Score = 45, LetterGrade = "D" };
        db.GradeRecords.AddRange(eGrade, fGrade); await db.SaveChangesAsync();

        var service = new SettingsService(db, new InstituteCache());
        await service.SaveAsync("grade-rules", new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "60", ["eMinimum"] = "50" }, CancellationToken.None);

        Assert.Equal("E", eGrade.LetterGrade);
        Assert.Equal("F", fGrade.LetterGrade);
        Assert.Contains(db.AuditLogs, log => log.Type == "Settings" && log.Subject == "grade-rules");
        Assert.Contains(db.Notifications, notification => notification.Title == "Configuration updated");
    }

    [Fact]
    public async Task Grade_thresholds_must_descend_through_E()
    {
        await using var db = CreateContext();
        db.SystemSettings.AddRange(Settings("grade-rules", new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "60", ["eMinimum"] = "50" }));
        await db.SaveChangesAsync();
        var service = new SettingsService(db, new InstituteCache());

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync("grade-rules", new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "50", ["eMinimum"] = "60" }, CancellationToken.None));
    }

    private static IEnumerable<SystemSetting> Settings(string section, Dictionary<string, string> values) => values.Select(item => new SystemSetting { Section = section, Key = item.Key, Value = item.Value });
    private static InstituteDbContext CreateContext() => new(new DbContextOptionsBuilder<InstituteDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
