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
        var rows = await db.Courses
            .AsNoTracking()
            .Where(course => course.IsActive)
            .GroupJoin(
                db.CourseAssignments
                    .AsNoTracking()
                    .Where(assignment =>
                        assignment.AcademicYear == period.AcademicYear
                        && assignment.Semester == period.Semester
                        && assignment.Status != "Removed"),
                course => course.Id,
                assignment => assignment.CourseId,
                (course, assignments) => new
                {
                    course,
                    assignment = assignments.FirstOrDefault()
                })
            .Select(row => new
            {
                row.course,
                row.assignment,
                department = row.assignment == null ? null : row.assignment.Department,
                teacher = row.assignment == null ? null : row.assignment.Teacher
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row =>
                row.assignment is not null
                && (!departmentId.HasValue || row.assignment.DepartmentId == departmentId)
                && (!year.HasValue || row.assignment.YearLevel == year)
                && Matches(search, row.course.CourseCode, row.course.Name, row.department?.Name, row.teacher?.FullName))
            .Select(row => Item(
                row.course.Id,
                ("courseCode", row.course.CourseCode),
                ("name", row.course.Name),
                ("departmentId", row.assignment?.DepartmentId.ToString() ?? ""),
                ("department", row.department?.Name ?? "Unassigned"),
                ("teacherId", row.assignment?.TeacherId.ToString() ?? ""),
                ("teacher", row.teacher?.FullName ?? "Unassigned"),
                ("year", row.assignment?.YearLevel.ToString() ?? ""),
                ("capacity", row.assignment?.Capacity.ToString() ?? ""),
                ("status", row.assignment?.Status ?? "Unassigned"),
                ("academicYear", period.AcademicYear),
                ("semester", period.Semester)))
            .ToList();
    }
}
