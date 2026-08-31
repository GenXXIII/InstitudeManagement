using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TeacherOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "teachers";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .Select(x => x.CourseId)
            .ToListAsync(cancellationToken);
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var currentTeacherIds = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled"
                && enrolledTimetableIds.Contains(x.Id)
                && courseAssignments.Contains(x.CourseId)
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt)
            .Select(x => x.TeacherId)
            .ToHashSetAsync(cancellationToken);
        if (!selection.IsRunning) currentTeacherIds.Clear();
        var teachers = await db.TeacherAssignments.AsNoTracking().Include(x => x.Teacher).Include(x => x.Department)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && currentTeacherIds.Contains(x.TeacherId)
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .OrderBy(x => x.Teacher!.TeacherCode)
            .ToListAsync(cancellationToken);
        var rows = teachers.Where(x => x.Teacher is not null).Select(x => new TeacherOperationDto(x.TeacherId, x.Teacher!.FullName, x.Teacher.TeacherCode, x.Department?.Name ?? "—", TeacherPresence.Attendance(x.Teacher.Status, x.Status)))
            .OrderBy(x => AttendancePriority(x.Status))
            .ThenBy(x => x.TeacherCode)
            .ToList();
        var timing = selection.IsRunning ? "Current" : "Next";
        var metrics = new List<MetricDto> { new("Scheduled", rows.Count.ToString(), $"{timing} timetable period"), new("Present", rows.Count(x => x.Status == "Present").ToString(), selection.IsRunning ? "Teaching now" : "Assigned next", "green"), new("Permission", rows.Count(x => x.Status == "Permission").ToString(), "Approved absence", "amber"), new("Absent", rows.Count(x => x.Status == "Absent").ToString(), "Not present", "red") };
        return new OperationDto(Module, $"Teacher operations · {context.Scope}", $"Teachers assigned to the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}), ordered by attendance.", metrics, context.Activity, context.Attention, Teachers: rows);
    }

    private static int AttendancePriority(string status) => status switch { "Present" => 0, "Permission" => 1, "Absent" => 2, _ => 3 };
}
