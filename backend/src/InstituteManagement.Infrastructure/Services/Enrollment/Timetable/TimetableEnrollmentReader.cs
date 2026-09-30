using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentReader(InstituteDbContext db)
{
    public async Task<PagedResult<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        PageRequest page,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var query = db.TimetableEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.ScheduleEntry)
            .Include(enrollment => enrollment.Course)
                .ThenInclude(course => course!.Department)
            .Include(enrollment => enrollment.Teacher)
            .Include(enrollment => enrollment.Classroom)
            .Where(enrollment =>
                enrollment.Status == "Active"
                && enrollment.ScheduleEntry != null
                && (!departmentId.HasValue || enrollment.Course == null || enrollment.Course.DepartmentId == departmentId)
                && (!year.HasValue || enrollment.YearLevel == year));
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var dateSearch = DateTime.TryParse(term, out var searchedDate);
            query = query.Where(enrollment =>
                enrollment.EnrollmentCode.Contains(term)
                || enrollment.ScheduleEntry!.TimetableCode.Contains(term)
                || enrollment.Course!.CourseCode.Contains(term)
                || enrollment.Course.Name.Contains(term)
                || enrollment.Teacher!.TeacherCode.Contains(term)
                || enrollment.Teacher.FullName.Contains(term)
                || enrollment.Classroom!.ClassroomCode.Contains(term)
                || enrollment.ScheduleEntry.Shift.Contains(term)
                || enrollment.Course.Department!.Name.Contains(term)
                || enrollment.Semester.Contains(term)
                || dateSearch && enrollment.CreateAt.Date == searchedDate.Date);
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var enrollments = await query
            .OrderBy(enrollment => enrollment.ScheduleEntry!.DayOfWeek == DayOfWeek.Monday ? 0
                : enrollment.ScheduleEntry.DayOfWeek == DayOfWeek.Tuesday ? 1
                : enrollment.ScheduleEntry.DayOfWeek == DayOfWeek.Wednesday ? 2
                : enrollment.ScheduleEntry.DayOfWeek == DayOfWeek.Thursday ? 3
                : enrollment.ScheduleEntry.DayOfWeek == DayOfWeek.Friday ? 4
                : enrollment.ScheduleEntry.DayOfWeek == DayOfWeek.Saturday ? 5 : 6)
            .ThenBy(enrollment => enrollment.ScheduleEntry!.Shift == "Morning" ? 0 : enrollment.ScheduleEntry.Shift == "Afternoon" ? 1 : enrollment.ScheduleEntry.Shift == "Evening" ? 2 : enrollment.ScheduleEntry.Shift == "Weekend" ? 3 : 4)
            .ThenBy(enrollment => enrollment.ScheduleEntry!.StartsAt)
            .ThenBy(enrollment => enrollment.ScheduleEntry!.TimetableCode)
            .ThenBy(enrollment => enrollment.Id)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        var items = enrollments
            .Select(enrollment =>
            {
                var resolvedDepartmentId = enrollment.Course?.DepartmentId;
                var resolvedDepartmentName = enrollment.Course?.Department?.Name;
                return new { Enrollment = enrollment, DepartmentId = resolvedDepartmentId, DepartmentName = resolvedDepartmentName };
            })
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
        return PagedResult<EnrollmentItemDto>.Create(items, page, totalCount);
    }
}
