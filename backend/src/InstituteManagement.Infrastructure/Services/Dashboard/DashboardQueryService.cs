using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Dashboard;

public sealed class DashboardQueryService(InstituteDbContext db, InstituteCache cache) : IDashboardQueryService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        var cached = await cache.ReadDashboardAsync<DashboardDto>();
        if (cached is not null) return cached;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentDay = DateTime.Today.DayOfWeek;
        var studentCount = await db.Students.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var teacherCount = await db.Teachers.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var courseCount = await db.Courses.AsNoTracking().CountAsync(x => x.IsActive, ct);
        var classroomCount = await db.Classrooms.AsNoTracking().CountAsync(x => x.Status != "Inactive", ct);
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => x.Date == today).ToListAsync(ct);
        var grades = await db.GradeRecords.AsNoTracking().Select(x => x.Score).ToListAsync(ct);
        var scheduleRows = await db.ScheduleEntries.AsNoTracking().Where(x => x.DayOfWeek == currentDay && x.Status != "Cancelled").OrderBy(x => x.StartsAt).Take(5).Select(x => new { x.StartsAt, Course = x.Course!.Name, Classroom = x.Classroom!.Code, x.Status }).ToListAsync(ct);
        var schedule = scheduleRows.Select(x => new StatusItemDto(x.StartsAt.ToString("HH:mm"), x.Course, x.Classroom, x.Status)).ToList();
        var notifications = await db.Notifications.AsNoTracking().Where(x => !x.IsRead).Take(4).Select(x => new ActivityDto("Now", x.Title, x.Message, x.Severity.ToLower())).ToListAsync(ct);
        var activityRows = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(5).Select(x => new { x.CreatedAtUtc, x.Action, x.Subject }).ToListAsync(ct);
        var activity = activityRows.Select(x => new ActivityDto(x.CreatedAtUtc.ToString("HH:mm"), x.Action, x.Subject, "blue")).ToList();
        var departments = await db.Departments.AsNoTracking().Where(x => x.IsActive).Take(5).Select(x => new StatusItemDto(x.Code, x.Name, x.Head, "Healthy")).ToListAsync(ct);
        var present = attendance.Count(x => x.Status is "Present" or "Late");
        var attendanceRate = attendance.Count == 0 ? 0 : Math.Round(present * 100m / attendance.Count, 1);

        var result = new DashboardDto(
            [new("Total students", studentCount.ToString("N0"), "Currently enrolled"), new("Teaching staff", teacherCount.ToString("N0"), "Active faculty", "violet"), new("Active courses", courseCount.ToString("N0"), "Current catalog", "green"), new("Classrooms", classroomCount.ToString("N0"), "Available facilities", "cyan"), new("Attendance today", $"{attendanceRate}%", $"{present:N0} checked in", "amber")],
            attendanceRate, 2.4m,
            [new("Present", attendance.Count(x => x.Status == "Present").ToString(), "Checked in", "Present"), new("Late", attendance.Count(x => x.Status == "Late").ToString(), "After threshold", "Late"), new("Absent", attendance.Count(x => x.Status == "Absent").ToString(), "Not checked in", "Absent")],
            schedule,
            [new("Mon", 82), new("Tue", 85), new("Wed", 84), new("Thu", 88), new("Fri", attendanceRate)],
            notifications,
            activity,
            departments,
            [new("A", Percentage(grades, 90, 101)), new("B", Percentage(grades, 80, 90)), new("C", Percentage(grades, 70, 80)), new("D", Percentage(grades, 60, 70)), new("F", Percentage(grades, 0, 60))]);

        await cache.WriteDashboardAsync(result);
        return result;
    }

    private static decimal Percentage(List<decimal> values, decimal min, decimal max) =>
        values.Count == 0 ? 0 : Math.Round(values.Count(x => x >= min && x < max) * 100m / values.Count, 1);
}
