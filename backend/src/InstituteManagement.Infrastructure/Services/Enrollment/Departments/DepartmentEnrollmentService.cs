using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Departments;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Departments;

internal sealed class DepartmentEnrollmentService : IDepartmentEnrollmentService
{
    private readonly InstituteDbContext db;
    private readonly EnrollmentSettingsReader settings;

    public DepartmentEnrollmentService(InstituteDbContext db, EnrollmentSettingsReader settings)
    {
        this.db = db;
        this.settings = settings;
    }

    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        await GetAsync(search, departmentId, year, await settings.CurrentPeriodAsync(cancellationToken), cancellationToken);

    private async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(department =>
                department.IsActive
                && (!departmentId.HasValue || department.Id == departmentId))
            .ToListAsync(cancellationToken);
        var students = await db.StudentEnrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active"
                && (!year.HasValue || enrollment.YearLevel == year))
            .ToListAsync(cancellationToken);
        var teachers = await db.TeacherAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.AcademicYear == period.AcademicYear
                && assignment.Semester == period.Semester
                && assignment.Status == "Assigned")
            .ToListAsync(cancellationToken);
        var courses = await db.CourseAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.AcademicYear == period.AcademicYear
                && assignment.Semester == period.Semester
                && assignment.Status == "Active"
                && (!year.HasValue || assignment.YearLevel == year))
            .ToListAsync(cancellationToken);
        var courseIds = courses.Select(assignment => assignment.CourseId).ToHashSet();
        var classes = await db.ScheduleEntries
            .AsNoTracking()
            .Where(entry =>
                entry.Status != "Cancelled"
                && courseIds.Contains(entry.CourseId)
                && (!year.HasValue || entry.YearLevel == year))
            .ToListAsync(cancellationToken);

        return departments
            .Where(department => Matches(search, department.DepartmentCode, department.Name))
            .Select(department =>
            {
                var departmentCourseIds = courses
                    .Where(course => course.DepartmentId == department.Id)
                    .Select(course => course.CourseId)
                    .ToHashSet();
                var departmentClasses = classes
                    .Where(entry => departmentCourseIds.Contains(entry.CourseId))
                    .ToList();

                return Item(
                    department.Id,
                    ("departmentCode", department.DepartmentCode),
                    ("name", department.Name),
                    ("year", year?.ToString() ?? "All"),
                    ("students", students.Count(enrollment => enrollment.DepartmentId == department.Id).ToString()),
                    ("teachers", teachers.Count(assignment =>
                        assignment.DepartmentId == department.Id
                        && departmentClasses.Any(entry => entry.TeacherId == assignment.TeacherId)).ToString()),
                    ("courses", departmentCourseIds.Count.ToString()),
                    ("classrooms", departmentClasses.Select(entry => entry.ClassroomId).Distinct().Count().ToString()),
                    ("weeklyClasses", departmentClasses.Count.ToString()),
                    ("status", "Active"),
                    ("academicYear", period.AcademicYear),
                    ("semester", period.Semester));
            })
            .ToList();
    }
}
