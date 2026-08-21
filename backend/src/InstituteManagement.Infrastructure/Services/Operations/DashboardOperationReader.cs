using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class DashboardOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "dashboard";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var students = db.Students.AsNoTracking().Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var teachers = db.Teachers.AsNoTracking().Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var rooms = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var courses = db.Courses.AsNoTracking().Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => x.Date == DateOnly.FromDateTime(DateTime.UtcNow) && x.Student!.Status != "Inactive" && (!departmentId.HasValue || x.Student.DepartmentId == departmentId)).Select(x => x.Status).ToListAsync(cancellationToken);
        var summary = new List<OperationSummaryDto>
        {
            new("Students", "Enrollment and student presence", (await students.CountAsync(cancellationToken)).ToString("N0"), $"{attendance.Count(x => x is "Present" or "Late"):N0} checked in today", attendance.Any(x => x == "Absent") ? "Review" : "Healthy", "/operation/students", "blue"),
            new("Teachers", "Faculty availability and assignments", (await teachers.CountAsync(cancellationToken)).ToString("N0"), $"{await teachers.CountAsync(x => x.Status == "Teaching", cancellationToken):N0} teaching now", "Current", "/operation/teachers", "violet"),
            new("Classrooms", "Room use and device readiness", (await rooms.CountAsync(cancellationToken)).ToString("N0"), $"{await rooms.CountAsync(x => x.Status == "Available", cancellationToken):N0} rooms available", await rooms.AnyAsync(x => !x.DeviceOnline, cancellationToken) ? "Review" : "Healthy", "/operation/classrooms", "cyan"),
            new("Courses", "Active academic course delivery", (await courses.CountAsync(cancellationToken)).ToString("N0"), "Weekly timetable available", "Active", "/operation/courses", "green")
        };
        var metrics = summary.Select(x => new MetricDto(x.Module, x.Value, x.Status, x.Tone)).ToList();
        return new OperationDto(Module, $"Institute operations dashboard · {context.Scope}", "A concise institute-wide summary. Open any operation to review its complete live details.", metrics, context.Activity, context.Attention, Summary: summary);
    }
}
