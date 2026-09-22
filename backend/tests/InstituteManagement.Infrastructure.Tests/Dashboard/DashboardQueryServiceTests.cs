using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Dashboard;

public sealed class DashboardQueryServiceTests
{
    [Fact]
    public async Task Attendance_trend_uses_the_requested_time_granularity()
    {
        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new InstituteDbContext(options);
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var sampleHour = Math.Max(0, now.Hour - 1);

        db.SystemSettings.Add(new SystemSetting { Section = "system", Key = "timeZone", Value = "UTC" });
        db.AttendanceRecords.AddRange(
            Attendance("ATT-DASH-1", Guid.NewGuid(), today, sampleHour, "Present"),
            Attendance("ATT-DASH-2", Guid.NewGuid(), today, sampleHour, "Absent"),
            Attendance("ATT-DASH-3", Guid.NewGuid(), today.AddYears(-2), 8, "Present"));
        await db.SaveChangesAsync();

        var service = new DashboardQueryService(db, new InstituteCache());
        var daily = await service.GetAsync("daily", CancellationToken.None);
        var weekly = await service.GetAsync("weekly", CancellationToken.None);
        var monthly = await service.GetAsync("monthly", CancellationToken.None);
        var yearly = await service.GetAsync("yearly", CancellationToken.None);
        var allTime = await service.GetAsync("all", CancellationToken.None);

        Assert.Equal(now.Hour + 1, daily.AttendanceTrend.Count);
        Assert.Equal($"{sampleHour:00}:00", daily.AttendanceTrend[sampleHour].Label);
        Assert.Equal(50m, daily.AttendanceTrend[sampleHour].Value);
        Assert.Equal(((int)today.DayOfWeek + 6) % 7 + 1, weekly.AttendanceTrend.Count);
        Assert.Equal(today.Day, monthly.AttendanceTrend.Count);
        Assert.Equal(today.Month, yearly.AttendanceTrend.Count);
        Assert.Equal(
            Enumerable.Range(today.Year - 2, 3).Select(year => year.ToString()),
            allTime.AttendanceTrend.Select(point => point.Label));
    }

    private static AttendanceRecord Attendance(string code, Guid studentId, DateOnly date, int hour, string status) => new()
    {
        AttendanceCode = code,
        StudentId = studentId,
        Date = date,
        CheckedInAt = new TimeOnly(hour, 15),
        Status = status,
        AcademicYear = $"{date.Year}–{date.Year + 1}",
        Term = "Semester 1"
    };
}
