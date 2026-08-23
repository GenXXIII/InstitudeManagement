using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
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
        var rooms = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive");
        var courses = db.Courses.AsNoTracking().Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var today = DateOnly.FromDateTime(localNow);
        var time = TimeOnly.FromDateTime(localNow);
        var checkedInStudents = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.Date == today && x.Status != "Absent" && x.Student!.Status != "Inactive" && (!departmentId.HasValue || x.Student.DepartmentId == departmentId))
            .Select(x => x.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
        var running = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.Status != "Completed" && x.DayOfWeek == localNow.DayOfWeek && x.StartsAt <= time && x.EndsAt > time && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .Select(x => new { x.TeacherId, x.ClassroomId, x.CourseId })
            .ToListAsync(cancellationToken);
        var studentTotal = await students.CountAsync(cancellationToken);
        var teacherTotal = await teachers.CountAsync(cancellationToken);
        var roomTotal = await rooms.CountAsync(cancellationToken);
        var courseTotal = await courses.CountAsync(cancellationToken);
        var runningTeachers = running.Select(x => x.TeacherId).Distinct().Count();
        var occupiedRooms = running.Select(x => x.ClassroomId).Distinct().Count();
        var runningCourses = running.Select(x => x.CourseId).Distinct().Count();
        var summary = new List<OperationSummaryDto>
        {
            new("Students", "Enrollment and student presence", $"{checkedInStudents:N0} / {studentTotal:N0}", "Came to school today / active students", checkedInStudents > 0 ? "Live" : "Waiting", "/operation/students", "blue"),
            new("Teachers", "Faculty availability and assignments", $"{runningTeachers:N0} / {teacherTotal:N0}", "Teaching now / active teachers", runningTeachers > 0 ? "Running" : "Waiting", "/operation/teachers", "violet"),
            new("Classrooms", "Room use and device readiness", $"{occupiedRooms:N0} / {roomTotal:N0}", "In study now / institute rooms", await rooms.AnyAsync(x => !x.DeviceOnline, cancellationToken) ? "Review" : occupiedRooms > 0 ? "Running" : "Ready", "/operation/classrooms", "cyan"),
            new("Courses", "Active academic course delivery", $"{runningCourses:N0} / {courseTotal:N0}", "Running now / active courses", runningCourses > 0 ? "Running" : "Waiting", "/operation/courses", "green")
        };
        var metrics = summary.Select(x => new MetricDto(x.Module, x.Value, x.Status, x.Tone)).ToList();
        return new OperationDto(Module, $"Institute operations dashboard · {context.Scope}", "One joined live view of students, teachers, classrooms, and courses. See current work without opening four separate pages.", metrics, context.Activity, context.Attention, Summary: summary);
    }
}
