using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Dashboard;

public sealed class DashboardQueryService(InstituteDbContext db, InstituteCache cache) : IDashboardQueryService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        var cached = await cache.ReadDashboardAsync<DashboardDto>(ct);
        if (cached is not null) return cached;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var trendStart = today.AddDays(-4);
        var currentDay = DateTime.Today.DayOfWeek;
        var studentCount = await db.Students.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var teacherCount = await db.Teachers.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var courseCount = await db.Courses.AsNoTracking().CountAsync(x => x.IsActive, ct);
        var classroomCount = await db.Classrooms.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var attendanceWindow = await db.AttendanceRecords.AsNoTracking().Where(x => x.Date >= trendStart && x.Date <= today).ToListAsync(ct);
        var attendance = attendanceWindow.Where(x => x.Date == today).ToList();
        var period = await db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
        var term = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
        var autoPercentageValue = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "attendance-rules" && x.Key == "autoPercentage").Select(x => x.Value).FirstOrDefaultAsync(ct);
        var autoPercentage = !bool.TryParse(autoPercentageValue, out var calculatePercentage) || calculatePercentage;
        var grades = await db.GradeRecords.AsNoTracking().Where(x => x.AcademicYear == academicYear && x.Term == term).Select(x => x.Score).ToListAsync(ct);
        var gradeSettings = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "grade-rules").ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        var gradeScale = GradeThresholds.From(gradeSettings);
        var scheduleRows = await db.ScheduleEntries.AsNoTracking().Where(x => x.DayOfWeek == currentDay && x.Status != "Cancelled").OrderBy(x => x.StartsAt).Take(5).Select(x => new { x.StartsAt, Course = x.Course!.Name, Classroom = x.Classroom!.ClassroomCode, x.Status }).ToListAsync(ct);
        var schedule = scheduleRows.Select(x => new StatusItemDto(x.StartsAt.ToString("HH:mm"), x.Course, x.Classroom, x.Status)).ToList();
        var notifications = await db.Notifications.AsNoTracking().Where(x => !x.IsRead).Take(4).Select(x => new ActivityDto("Now", x.Title, x.Message, x.Severity.ToLower(), x.NotificationCode)).ToListAsync(ct);
        var activityRows = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreateAt).Take(5).Select(x => new { x.CreateAt, x.Action, x.Subject }).ToListAsync(ct);
        var activity = activityRows.Select(x => new ActivityDto(x.CreateAt.ToString("HH:mm"), x.Action, x.Subject, "blue")).ToList();
        var departments = await db.Departments.AsNoTracking().Where(x => x.IsActive).Take(5).Select(x => new StatusItemDto(x.DepartmentCode, x.Name, x.Head, "Healthy")).ToListAsync(ct);
        var present = attendance.Count(x => x.Status is "Present" or "Late");
        var attendanceRate = autoPercentage ? AttendanceRate(attendance) : 0;
        var attendanceTrend = Enumerable.Range(0, 5)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date => new ChartPointDto(date.ToString("ddd"), autoPercentage ? AttendanceRate(attendanceWindow.Where(x => x.Date == date)) : 0))
            .ToList();
        var attendanceChange = attendanceTrend.Count < 2 ? 0 : attendanceTrend[^1].Value - attendanceTrend[^2].Value;
        var averageGrade = grades.Count == 0 ? 0 : Math.Round(grades.Average(), 1);

        var result = new DashboardDto(
            [new("Total students", studentCount.ToString("N0"), "Currently enrolled"), new("Teaching staff", teacherCount.ToString("N0"), "Active faculty", "violet"), new("Active courses", courseCount.ToString("N0"), "Current catalog", "green"), new("Classrooms", classroomCount.ToString("N0"), "Available facilities", "cyan"), new("Attendance today", $"{attendanceRate}%", $"{present:N0} checked in", "amber")],
            attendanceRate, attendanceChange,
            [new("Present", attendance.Count(x => x.Status == "Present").ToString(), "Checked in", "Present"), new("Late", attendance.Count(x => x.Status == "Late").ToString(), "After threshold", "Late"), new("Absent", attendance.Count(x => x.Status == "Absent").ToString(), "Not checked in", "Absent")],
            schedule,
            attendanceTrend,
            notifications,
            activity,
            departments,
            averageGrade,
            [new("A", Percentage(grades, gradeScale.A, 101)), new("B", Percentage(grades, gradeScale.B, gradeScale.A)), new("C", Percentage(grades, gradeScale.C, gradeScale.B)), new("D", Percentage(grades, gradeScale.D, gradeScale.C)), new("E", Percentage(grades, gradeScale.E, gradeScale.D)), new("F", Percentage(grades, 0, gradeScale.E))]);

        await cache.WriteDashboardAsync(result, ct);
        return result;
    }

    private static decimal Percentage(List<decimal> values, decimal min, decimal max) =>
        values.Count == 0 ? 0 : Math.Round(values.Count(x => x >= min && x < max) * 100m / values.Count, 1);

    private static decimal AttendanceRate(IEnumerable<Domain.Entities.AttendanceRecord> records)
    {
        var values = records.ToList();
        return values.Count == 0 ? 0 : Math.Round(values.Count(x => x.Status is "Present" or "Late") * 100m / values.Count, 1);
    }
}
