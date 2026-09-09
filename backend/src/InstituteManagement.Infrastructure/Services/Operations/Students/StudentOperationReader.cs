using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Enrollment;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class StudentOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "students";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var enrollmentPeriod = source.Period;
        var currentTimetables = source.Timetables
            .Where(x => x.ScheduleEntry!.Shift == shift.Name
                && x.ScheduleEntry.DayOfWeek == selection.Date.DayOfWeek
                && x.ScheduleEntry.StartsAt == period.StartsAt
                && x.ScheduleEntry.EndsAt == period.EndsAt)
            .ToList();
        if (!selection.IsRunning) currentTimetables.Clear();
        var currentCohorts = currentTimetables
            .Select(OperationEnrollmentSourceService.TimetableCohort)
            .Where(cohort => cohort.HasValue)
            .Select(cohort => cohort!.Value)
            .ToHashSet();
        var runningCohorts = currentTimetables
            .Where(x => TeacherPresence.IsPresent(TeacherPresence.Attendance(x.Teacher?.Status)))
            .Select(OperationEnrollmentSourceService.TimetableCohort)
            .Where(cohort => cohort.HasValue)
            .Select(cohort => cohort!.Value)
            .ToHashSet();
        var coursesByCohort = currentTimetables
            .Select(x => new
            {
                Cohort = OperationEnrollmentSourceService.TimetableCohort(x),
                Course = x.Course!.Name
            })
            .Where(x => x.Cohort.HasValue)
            .GroupBy(x => x.Cohort!.Value)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group.Select(x => x.Course).Distinct().Order()));
        var enrollments = source.Students
            .Where(x =>
                x.Shift == shift.Name
                && currentCohorts.Contains(OperationEnrollmentSourceService.StudentCohort(x)))
            .OrderBy(x => x.Student!.StudentCode)
            .ToList();
        var ids = enrollments.Select(x => x.StudentId).ToList();
        var attendance = selection.IsRunning
            ? await db.AttendanceRecords.AsNoTracking()
                .Where(x => ids.Contains(x.StudentId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Term == enrollmentPeriod.Semester && x.Date == selection.Date)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ToListAsync(cancellationToken)
            : [];
        var status = attendance.GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => NormalizeAttendance(x.First().Status));
        var defaultStatus = selection.IsRunning ? "Absent" : "Scheduled";
        var rows = enrollments.Select(x => new StudentOperationDto(
                x.StudentId,
                x.Student!.FullName,
                x.Student.StudentCode,
                codeFormat.Derive(EnrollmentSource(x.EnrollmentCode, x.Student.StudentCode), "student", "operation"),
                x.Department?.Name ?? "—",
                coursesByCohort.GetValueOrDefault(OperationEnrollmentSourceService.StudentCohort(x), "—"),
                x.YearLevel,
                x.Shift,
                runningCohorts.Contains(OperationEnrollmentSourceService.StudentCohort(x)) ? status.GetValueOrDefault(x.StudentId, defaultStatus) : "Class not running"))
            .OrderBy(x => AttendancePriority(x.AttendanceStatus))
            .ThenBy(x => x.StudentCode)
            .ToList();
        var metrics = new List<MetricDto>
        {
            new("Scheduled", rows.Count.ToString(), $"{shift.Name} · {selection.Date:dddd}"),
            new("Present", rows.Count(x => x.AttendanceStatus == "Present").ToString(), selection.IsRunning ? "Real-time" : "Not started", "green"),
            new("Permission", rows.Count(x => x.AttendanceStatus == "Permission").ToString(), selection.IsRunning ? "Real-time" : "Not started", "amber"),
            new("Absent", rows.Count(x => x.AttendanceStatus == "Absent").ToString(), selection.IsRunning ? "Real-time" : "Not started", "red"),
            new("Class not running", rows.Count(x => x.AttendanceStatus == "Class not running").ToString(), "Teacher absent or permission", "red")
        };
        var state = selection.IsRunning ? "currently in progress" : "next";
        return new OperationDto(
            Module,
            $"Student operations · {context.Scope}",
            $"Students assigned to the {state} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).",
            metrics,
            context.Activity,
            context.Attention,
            Students: rows);
    }

    private static string NormalizeAttendance(string status) => status switch
    {
        "Present" or "Late" => "Present",
        "Excused" or "Permission" => "Permission",
        _ => "Absent"
    };

    private static int AttendancePriority(string status) => status switch
    {
        "Present" => 0,
        "Permission" => 1,
        "Absent" => 2,
        "Class not running" => 3,
        "Scheduled" => 4,
        _ => 5
    };

    private static string EnrollmentSource(string enrollmentCode, string managementCode) =>
        string.IsNullOrWhiteSpace(enrollmentCode) ? managementCode : enrollmentCode;

}
