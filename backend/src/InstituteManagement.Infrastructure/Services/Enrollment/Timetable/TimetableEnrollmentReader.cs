using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var enrollments = await db.TimetableEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.ScheduleEntry)
            .Include(enrollment => enrollment.Course)
                .ThenInclude(course => course!.Department)
            .Include(enrollment => enrollment.Teacher)
            .Include(enrollment => enrollment.Classroom)
            .Where(enrollment =>
                enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active")
            .ToListAsync(cancellationToken);
        var courseIds = enrollments.Select(enrollment => enrollment.CourseId).Distinct().ToList();
        var assignments = (await db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Where(assignment =>
                courseIds.Contains(assignment.CourseId)
                && assignment.AcademicYear == period.AcademicYear
                && assignment.Semester == period.Semester
                && assignment.Status == "Active")
            .OrderByDescending(assignment => assignment.UpdatedAtUtc)
            .ToListAsync(cancellationToken))
            .GroupBy(assignment => assignment.CourseId)
            .ToDictionary(group => group.Key, group => group.First());

        return enrollments
            .Where(enrollment => enrollment.ScheduleEntry is not null)
            .Select(enrollment =>
            {
                assignments.TryGetValue(enrollment.CourseId, out var assignment);
                var resolvedDepartmentId = assignment?.DepartmentId ?? enrollment.Course?.DepartmentId;
                var resolvedDepartmentName = assignment?.Department?.Name ?? enrollment.Course?.Department?.Name;
                return new { Enrollment = enrollment, DepartmentId = resolvedDepartmentId, DepartmentName = resolvedDepartmentName };
            })
            .Where(row =>
                (!departmentId.HasValue || !row.DepartmentId.HasValue || row.DepartmentId == departmentId)
                && (!year.HasValue || row.Enrollment.YearLevel == year)
                && Matches(
                    search,
                    row.Enrollment.EnrollmentCode,
                    row.Enrollment.ScheduleEntry!.TimetableCode,
                    row.Enrollment.Course?.CourseCode,
                    row.Enrollment.Course?.Name,
                    row.Enrollment.Teacher?.TeacherCode,
                    row.Enrollment.Teacher?.FullName,
                    row.Enrollment.Classroom?.ClassroomCode,
                    row.Enrollment.ScheduleEntry.Shift,
                    row.DepartmentName,
                    row.Enrollment.Semester,
                    row.Enrollment.CreateAt.ToString("yyyy-MM-dd")))
            .Select(row =>
            {
                return TimetableEnrollmentItemFactory.Create(
                    row.Enrollment.ScheduleEntry!,
                    row.Enrollment,
                    row.DepartmentId,
                    row.DepartmentName);
            })
            .ToList();
    }
}
