using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Departments;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Departments;

internal sealed class DepartmentEnrollmentService(InstituteDbContext db, EnrollmentSettingsReader settings) : IDepartmentEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken)
    {
        _ = year;
        var period = await settings.CurrentPeriodAsync(cancellationToken);
        var departments = await db.Departments
            .AsNoTracking()
            .Where(department => department.IsActive && (!departmentId.HasValue || department.Id == departmentId))
            .ToListAsync(cancellationToken);
        return departments
            .Where(department => Matches(search, department.DepartmentCode, department.Name))
            .Select(department => Item(
                department.Id,
                ("departmentCode", department.DepartmentCode),
                ("name", department.Name),
                ("status", "Active"),
                ("academicYear", period.AcademicYear),
                ("semester", period.Semester),
                ("periodState", "Current"),
                ("createAt", department.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }
}
