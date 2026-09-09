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
                enrollment.Status == "Active")
            .ToListAsync(cancellationToken);
        return enrollments
            .Where(enrollment => enrollment.ScheduleEntry is not null)
            .Select(enrollment =>
            {
                var resolvedDepartmentId = enrollment.Course?.DepartmentId;
                var resolvedDepartmentName = enrollment.Course?.Department?.Name;
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
                    row.DepartmentName,
                    period);
            })
            .ToList();
    }
}
