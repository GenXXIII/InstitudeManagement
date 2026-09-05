using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal sealed class StudentEnrollmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var rows = await db.Students
            .AsNoTracking()
            .Where(student => student.Status != "Inactive")
            .GroupJoin(
                db.StudentEnrollments
                    .AsNoTracking()
                    .Where(enrollment =>
                        enrollment.AcademicYear == period.AcademicYear
                        && enrollment.Semester == period.Semester
                        && enrollment.Status != "Removed"),
                student => student.Id,
                enrollment => enrollment.StudentId,
                (student, enrollments) => new
                {
                    student,
                    enrollment = enrollments.FirstOrDefault()
                })
            .Select(row => new
            {
                row.student,
                row.enrollment,
                department = row.enrollment == null ? null : row.enrollment.Department
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row =>
                (!departmentId.HasValue || row.enrollment?.DepartmentId == departmentId)
                && (!year.HasValue || row.enrollment?.YearLevel == year)
                && Matches(search, row.enrollment?.EnrollmentCode, row.student.StudentCode, row.student.FullName, row.department?.Name))
            .Select(row => Item(
                row.student.Id,
                ("enrollmentCode", row.enrollment?.EnrollmentCode ?? ""),
                ("studentCode", row.student.StudentCode),
                ("name", row.student.FullName),
                ("email", row.student.Email),
                ("photoDataUrl", row.student.PhotoDataUrl),
                ("departmentId", row.enrollment?.DepartmentId.ToString() ?? ""),
                ("department", row.department?.Name ?? "Unassigned"),
                ("year", row.enrollment?.YearLevel.ToString() ?? ""),
                ("shift", row.enrollment?.Shift ?? ""),
                ("status", row.enrollment?.Status ?? "Unassigned"),
                ("academicYear", period.AcademicYear),
                ("semester", period.Semester)))
            .ToList();
    }
}
