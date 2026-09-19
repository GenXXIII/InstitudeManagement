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
        var students = await db.Students
            .AsNoTracking()
            .Where(student => student.Status != "Inactive")
            .ToListAsync(cancellationToken);
        var studentById = students.ToDictionary(student => student.Id);
        var studentIds = students.Select(student => student.Id).ToList();
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Department)
            .Where(enrollment => enrollment.Status != "Removed" && studentIds.Contains(enrollment.StudentId))
            .ToListAsync(cancellationToken);
        var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();
        var paymentByEnrollment = await db.FinancialAccounts.AsNoTracking()
            .Where(account => enrollmentIds.Contains(account.StudentEnrollmentId))
            .ToDictionaryAsync(account => account.StudentEnrollmentId, account => account.Status, cancellationToken);
        var annualPaid = await db.FinancialAccounts.AsNoTracking()
            .Where(account =>
                studentIds.Contains(account.StudentId)
                && account.Semester == "Semester 1"
                && account.PaymentPlan == "Year"
                && account.DeclaredAtUtc != null
                && account.Status == "Paid")
            .Select(account => new { account.StudentId, account.AcademicYear })
            .ToListAsync(cancellationToken);
        var annualCoverage = annualPaid.Select(account => (account.StudentId, account.AcademicYear)).ToHashSet();

        return enrollments
            .Where(enrollment =>
                (!departmentId.HasValue || enrollment.DepartmentId == departmentId)
                && (!year.HasValue || enrollment.YearLevel == year)
                && Matches(search, enrollment.EnrollmentCode, studentById[enrollment.StudentId].StudentCode, studentById[enrollment.StudentId].PublicId, studentById[enrollment.StudentId].FullName, enrollment.Department?.Name))
            .Select(enrollment =>
            {
                var student = studentById[enrollment.StudentId];
                return Item(
                    student.Id,
                    ("enrollmentCode", enrollment.EnrollmentCode),
                    ("studentCode", student.StudentCode),
                    ("publicId", student.PublicId),
                    ("name", student.FullName),
                    ("email", student.Email),
                    ("photoDataUrl", student.PhotoDataUrl),
                    ("departmentId", enrollment.DepartmentId.ToString()),
                    ("department", enrollment.Department?.Name ?? "Unassigned"),
                    ("year", enrollment.YearLevel.ToString()),
                    ("shift", enrollment.Shift),
                    ("status", enrollment.Status),
                    ("academicYear", enrollment.AcademicYear),
                    ("semester", enrollment.Semester),
                    ("periodState", IsCurrent(enrollment.AcademicYear, enrollment.Semester, period) ? "Current" : "Retained"),
                    ("paymentStatus", enrollment.Semester == "Semester 2" && annualCoverage.Contains((enrollment.StudentId, enrollment.AcademicYear))
                        ? "Paid"
                        : paymentByEnrollment.GetValueOrDefault(enrollment.Id, "Pending")),
                    ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));
            })
            .ToList();
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;
}
