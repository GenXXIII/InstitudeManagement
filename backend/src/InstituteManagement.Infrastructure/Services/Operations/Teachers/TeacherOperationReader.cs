using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Common;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TeacherOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "teachers";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var current = source.Timetables
            .Where(enrollment => enrollment.ScheduleEntry!.Shift == shift.Name
                && enrollment.ScheduleEntry.DayOfWeek == selection.Date.DayOfWeek
                && enrollment.ScheduleEntry.StartsAt == period.StartsAt
                && enrollment.ScheduleEntry.EndsAt == period.EndsAt)
            .ToList();
        if (!selection.IsRunning) current.Clear();
        var startedScheduleIds = await ClassSessionStartStateReader.GetStartedScheduleIdsAsync(
            db,
            DateOnly.FromDateTime(now),
            current.Select(enrollment => enrollment.ScheduleEntryId),
            cancellationToken);
        var teacherIds = current.Select(enrollment => enrollment.TeacherId).Distinct().ToList();
        var publicIds = await db.TeacherAssignments.AsNoTracking()
            .Where(assignment => teacherIds.Contains(assignment.TeacherId)
                && assignment.AcademicYear == source.Period.AcademicYear
                && assignment.Semester == source.Period.Semester
                && assignment.Status == "Assigned")
            .ToDictionaryAsync(assignment => assignment.TeacherId, assignment => assignment.PublicId, cancellationToken);
        var rows = current
            .GroupBy(enrollment => enrollment.TeacherId)
            .Select(group =>
            {
                var enrollment = group.First();
                var teacher = enrollment.Teacher!;
                var publicId = publicIds.GetValueOrDefault(teacher.Id, string.Empty);
                var departments = group.Select(item => item.Course!.Department?.Name ?? "—").Distinct().Order().ToList();
                var courses = group.Select(item => item.Course!.Name).Distinct().Order().ToList();
                return new TeacherOperationDto(
                    teacher.Id,
                    teacher.FullName,
                    teacher.TeacherCode,
                    codeFormat.Derive(teacher.TeacherCode, "teacher", "operation"),
                    publicId,
                    AttendanceIdentityQr.CreateTeacher(publicId),
                    string.Join(", ", departments),
                    string.Join(", ", courses),
                    TeacherPresence.ClassAttendance(
                        group.Any(item => startedScheduleIds.Contains(item.ScheduleEntryId)),
                        teacher.Status));
            })
            .OrderBy(x => AttendancePriority(x.Status))
            .ThenBy(x => x.TeacherCode)
            .ToList();
        var timing = selection.IsRunning ? "Current" : "Next";
        var metrics = new List<MetricDto> { new("Scheduled", rows.Count.ToString(), $"{timing} timetable period"), new("Present", rows.Count(x => x.Status == "Present").ToString(), selection.IsRunning ? "Teaching now" : "Assigned next", "green"), new("Permission", rows.Count(x => x.Status == "Permission").ToString(), "Approved absence", "amber"), new("Absent", rows.Count(x => x.Status == "Absent").ToString(), "Not present", "red") };
        return new OperationDto(Module, $"Teacher operations · {context.Scope}", $"Teachers assigned to the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}), ordered by attendance.", metrics, context.Activity, context.Attention, Teachers: rows);
    }

    private static int AttendancePriority(string status) => status switch { "Present" => 0, "Permission" => 1, "Absent" => 2, _ => 3 };

}
