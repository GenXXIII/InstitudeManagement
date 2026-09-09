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
        var courses = await db.Courses
            .AsNoTracking()
            .Where(course => course.IsActive)
            .ToListAsync(cancellationToken);
        var courseById = courses.ToDictionary(course => course.Id);
        var courseIds = courses.Select(course => course.Id).ToList();
        var assignments = await db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Include(assignment => assignment.Teacher)
            .Where(assignment => assignment.Status != "Removed" && courseIds.Contains(assignment.CourseId))
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || courseById[assignment.CourseId].YearLevel == year)
                && Matches(search, assignment.EnrollmentCode, courseById[assignment.CourseId].CourseCode, courseById[assignment.CourseId].Name, assignment.Department?.Name, assignment.Teacher?.FullName))
            .Select(assignment =>
            {
                var course = courseById[assignment.CourseId];
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
