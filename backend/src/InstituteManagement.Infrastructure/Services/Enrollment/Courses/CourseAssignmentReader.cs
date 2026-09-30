using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Courses;

internal sealed class CourseAssignmentReader(InstituteDbContext db)
{
    public async Task<PagedResult<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        PageRequest page,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var query = db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Course)
            .Include(assignment => assignment.Department)
            .Include(assignment => assignment.Teacher)
            .Where(assignment =>
                assignment.Status != "Removed"
                && assignment.Course != null
                && assignment.Course.IsActive
                && (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || assignment.Course.YearLevel == year));
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(assignment =>
                assignment.EnrollmentCode.Contains(term)
                || assignment.Course!.CourseCode.Contains(term)
                || assignment.Course.Name.Contains(term)
                || assignment.Department!.Name.Contains(term)
                || assignment.Teacher!.FullName.Contains(term));
        var totalCount = await query.CountAsync(cancellationToken);
        var assignments = await query
            .OrderBy(assignment => assignment.Course!.YearLevel)
            .ThenBy(assignment => assignment.Course!.CourseCode)
            .ThenBy(assignment => assignment.Id)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = assignments.Select(assignment =>
            {
                var course = assignment.Course!;
                return Item(
                    course.Id,
                    ("enrollmentCode", assignment.EnrollmentCode),
                    ("courseCode", course.CourseCode),
                    ("name", course.Name),
                    ("departmentId", assignment.DepartmentId.ToString()),
                    ("department", assignment.Department?.Name ?? "Unassigned"),
                    ("teacherId", assignment.TeacherId?.ToString() ?? ""),
                    ("teacher", assignment.Teacher?.FullName ?? "Unassigned"),
                    ("year", course.YearLevel.ToString()),
                    ("capacity", assignment.Capacity.ToString()),
                    ("status", assignment.Status),
                    ("academicYear", assignment.AcademicYear),
                    ("semester", assignment.Semester),
                    ("periodState", IsCurrent(assignment.AcademicYear, assignment.Semester, period) ? "Current" : "Retained"),
                    ("createAt", assignment.CreateAt.ToString("yyyy-MM-dd")));
            })
            .ToList();
        return PagedResult<EnrollmentItemDto>.Create(items, page, totalCount);
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;
}
