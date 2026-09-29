using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Courses;

internal sealed class CourseAssignmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignments = await db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Course)
            .Include(assignment => assignment.Department)
            .Include(assignment => assignment.Teacher)
            .Where(assignment =>
                assignment.Status != "Removed"
                && assignment.Course != null
                && assignment.Course.IsActive
                && (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || assignment.Course.YearLevel == year))
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                Matches(
                    search,
                    assignment.EnrollmentCode,
                    assignment.Course!.CourseCode,
                    assignment.Course.Name,
                    assignment.Department?.Name,
                    assignment.Teacher?.FullName))
            .Select(assignment =>
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
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;
}
