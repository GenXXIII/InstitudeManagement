using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Administration;

public sealed class SettingsCatalogSeederTests
{
    [Fact]
    public async Task Legacy_enrollment_codes_are_converted_once_to_occurrence_first_codes()
    {
        await using var db = CreateContext();
        db.SystemSettings.AddRange(
            new SystemSetting { Section = "code-formats", Key = "studentEnrollmentPrefix", Value = "ESTU" },
            new SystemSetting { Section = "code-formats", Key = "timetableEnrollmentPrefix", Value = "ETIM" });
        var student = new Student { StudentCode = "STU-16", FullName = "Student Sixteen" };
        var schedule = new ScheduleEntry { TimetableCode = "TIM-16", Shift = "Morning", StartsAt = new TimeOnly(7, 30), EndsAt = new TimeOnly(9, 0) };
        var studentEnrollment = new StudentEnrollment
        {
            EnrollmentCode = "STU-16-ESTU-16",
            StudentId = student.Id,
            Student = student,
            DepartmentId = Guid.NewGuid(),
            AcademicYear = "2026–2027",
            Semester = "Semester 1"
        };
        var firstTimetableEnrollment = TimetableEnrollment(schedule, "TIM-16-ETIM-16", new DateTime(2026, 8, 1));
        var secondTimetableEnrollment = TimetableEnrollment(schedule, "TIM-16-ETIM-17", new DateTime(2027, 2, 1));
        db.AddRange(student, schedule, studentEnrollment, firstTimetableEnrollment, secondTimetableEnrollment);
        await db.SaveChangesAsync();

        await SettingsCatalogSeeder.SeedMissingAsync(db);

        Assert.Equal("ENR-1-STU-16", studentEnrollment.EnrollmentCode);
        Assert.Equal($"STU-{studentEnrollment.Id:N}".ToUpperInvariant(), studentEnrollment.PublicId);
        Assert.Equal("ENR-1-TIM-16", firstTimetableEnrollment.EnrollmentCode);
        Assert.Equal("ENR-2-TIM-16", secondTimetableEnrollment.EnrollmentCode);
        Assert.Equal("ENR", db.SystemSettings.Single(item => item.Key == "studentEnrollmentPrefix").Value);
        Assert.Equal("ENR", db.SystemSettings.Single(item => item.Key == "timetableEnrollmentPrefix").Value);
    }

    [Fact]
    public async Task Legacy_finance_providers_are_removed_and_bakong_is_preserved()
    {
        await using var db = CreateContext();
        db.SystemSettings.AddRange(
            new SystemSetting { Section = "finance", Key = "paymentMethods", Value = "Cash,ABA,ACLEDA,Wing,Bank Transfer,Other" },
            new SystemSetting { Section = "finance", Key = "abaEnabled", Value = "true" },
            new SystemSetting { Section = "finance", Key = "acledaEnabled", Value = "true" });
        await db.SaveChangesAsync();

        await SettingsCatalogSeeder.SeedMissingAsync(db);

        Assert.Equal("Cash,Bakong,Wing,Bank Transfer,Other", db.SystemSettings.Single(item => item.Key == "paymentMethods").Value);
        Assert.DoesNotContain(db.SystemSettings, item => item.Key.StartsWith("aba", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(db.SystemSettings, item => item.Key.StartsWith("acleda", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("true", db.SystemSettings.Single(item => item.Section == "finance" && item.Key == "mockPaymentEnabled").Value);
    }

    private static TimetableEnrollment TimetableEnrollment(ScheduleEntry schedule, string code, DateTime createdAt) =>
        new()
        {
            EnrollmentCode = code,
            ScheduleEntryId = schedule.Id,
            ScheduleEntry = schedule,
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            YearLevel = 1,
            AcademicYear = createdAt.Year == 2026 ? "2026–2027" : "2027–2028",
            Semester = "Semester 1",
            CreateAt = createdAt
        };

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
