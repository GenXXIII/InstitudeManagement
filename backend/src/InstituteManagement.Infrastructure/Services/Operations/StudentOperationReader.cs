using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class StudentOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "students";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking().Include(x => x.Department)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToDictionaryAsync(x => x.CourseId, cancellationToken);
        var courseIds = courseAssignments.Keys.ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var selectedSchedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Teacher)
            .Where(x => x.Status != "Cancelled"
                && enrolledTimetableIds.Contains(x.Id)
                && courseIds.Contains(x.CourseId)
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt)
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) selectedSchedules.Clear();
        var currentCohorts = selectedSchedules.Select(x => (courseAssignments[x.CourseId].DepartmentId, x.YearLevel)).ToHashSet();
        var teacherIds = selectedSchedules.Select(x => x.TeacherId).Distinct().ToList();
        var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned")
            .ToListAsync(cancellationToken);
        var runningCohorts = selectedSchedules.Where(schedule =>
        {
            var courseAssignment = courseAssignments[schedule.CourseId];
            var teacherAssignment = teacherAssignments
                .Where(item => item.TeacherId == schedule.TeacherId && (item.DepartmentId == courseAssignment.DepartmentId || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == courseAssignment.DepartmentId)
                .FirstOrDefault();
            return TeacherPresence.IsPresent(TeacherPresence.Attendance(schedule.Teacher?.Status, teacherAssignment?.Status));
        }).Select(x => (courseAssignments[x.CourseId].DepartmentId, x.YearLevel)).ToHashSet();
        var enrollments = await db.StudentEnrollments.AsNoTracking().Include(x => x.Student).Include(x => x.Department)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && x.Shift == shift.Name
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        enrollments = enrollments.Where(x => x.Student is not null && x.Student.Status != "Inactive" && currentCohorts.Contains((x.DepartmentId, x.YearLevel))).OrderBy(x => x.Student!.StudentCode).ToList();
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
                x.Department?.Name ?? "—",
                x.YearLevel,
                x.Shift,
                runningCohorts.Contains((x.DepartmentId, x.YearLevel)) ? status.GetValueOrDefault(x.StudentId, defaultStatus) : "Class not running"))
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
}
