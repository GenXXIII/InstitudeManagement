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
        var query = db.StudentEnrollments.AsNoTracking()
            .Where(enrollment =>
                enrollment.Status != "Removed"
                && enrollment.Student != null
                && enrollment.Student.Status != "Inactive"
                && (!departmentId.HasValue || enrollment.DepartmentId == departmentId)
                && (!year.HasValue || enrollment.YearLevel == year));

        var projectedEnrollments = await query
            .Select(enrollment => new StudentEnrollmentRow(
                enrollment.Id,
                enrollment.StudentId,
                enrollment.EnrollmentCode,
                enrollment.PublicId,
                enrollment.Student!.StudentCode,
                enrollment.Student.FullName,
                enrollment.Student.Email,
                enrollment.Student.PhotoDataUrl,
                enrollment.DepartmentId,
                enrollment.Department!.Name,
                enrollment.YearLevel,
                enrollment.Shift,
                enrollment.Status,
                enrollment.AcademicYear,
                enrollment.Semester,
                enrollment.CreateAt))
            .ToListAsync(cancellationToken);
        var enrollments = projectedEnrollments
            .Where(enrollment => Matches(
                search,
                enrollment.EnrollmentCode,
                enrollment.StudentCode,
                enrollment.PublicId,
                enrollment.FullName,
                enrollment.DepartmentName))
            .ToList();
        if (enrollments.Count == 0) return [];

        var studentIds = enrollments.Select(enrollment => enrollment.StudentId).Distinct().ToList();
        var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();
        var paymentByEnrollment = await db.FinancialAccounts.AsNoTracking()
            .Where(account => enrollmentIds.Contains(account.StudentEnrollmentId))
            .ToDictionaryAsync(
                account => account.StudentEnrollmentId,
                account => new PaymentState(account.Status, account.ClosedAtUtc),
                cancellationToken);
        var publishedPeriods = (await db.SemesterResultPublications.AsNoTracking()
                .Where(publication => studentIds.Contains(publication.StudentId))
                .Select(publication => new { publication.StudentId, publication.AcademicYear, publication.Term })
                .ToListAsync(cancellationToken))
            .Select(publication => (publication.StudentId, publication.AcademicYear, publication.Term))
            .ToHashSet();
        var studentsInCurrentPeriod = (await db.StudentEnrollments.AsNoTracking()
                .Where(enrollment =>
                    studentIds.Contains(enrollment.StudentId)
                    && enrollment.Status != "Removed"
                    && enrollment.AcademicYear == period.AcademicYear
                    && enrollment.Semester == period.Semester)
                .Select(enrollment => enrollment.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
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
            .Select(enrollment =>
            {
                var isCurrent = IsCurrent(enrollment.AcademicYear, enrollment.Semester, period);
                paymentByEnrollment.TryGetValue(enrollment.Id, out var payment);
                var nextPeriod = NextPeriodStatus(
                    enrollment.Status,
                    enrollment.YearLevel,
                    enrollment.Semester,
                    isCurrent,
                    !isCurrent && studentsInCurrentPeriod.Contains(enrollment.StudentId),
                    payment?.Status,
                    payment?.ClosedAtUtc,
                    publishedPeriods.Contains((enrollment.StudentId, enrollment.AcademicYear, enrollment.Semester)));
                return Item(
                    enrollment.StudentId,
                    ("enrollmentCode", enrollment.EnrollmentCode),
                    ("studentCode", enrollment.StudentCode),
                    ("publicId", enrollment.PublicId),
                    ("name", enrollment.FullName),
                    ("email", enrollment.Email),
                    ("photoDataUrl", enrollment.PhotoDataUrl),
                    ("departmentId", enrollment.DepartmentId.ToString()),
                    ("department", enrollment.DepartmentName ?? "Unassigned"),
                    ("year", enrollment.YearLevel.ToString()),
                    ("shift", enrollment.Shift),
                    ("status", enrollment.Status),
                    ("academicYear", enrollment.AcademicYear),
                    ("semester", enrollment.Semester),
                    ("periodState", isCurrent ? "Current" : "Retained"),
                    ("paymentStatus", enrollment.Semester == "Semester 2" && annualCoverage.Contains((enrollment.StudentId, enrollment.AcademicYear))
                        ? "Paid"
                        : payment?.Status ?? "Pending"),
                    ("nextPeriodStatus", nextPeriod.Status),
                    ("nextPeriodState", nextPeriod.State),
                    ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));
            })
            .ToList();
    }

    private static (string Status, string State) NextPeriodStatus(
        string enrollmentStatus,
        int yearLevel,
        string semester,
        bool isCurrent,
        bool hasCurrentEnrollment,
        string? paymentStatus,
        DateTime? paymentClosedAtUtc,
        bool resultPublished)
    {
        if (!isCurrent && hasCurrentEnrollment) return ("Moved", "available");
        if (enrollmentStatus != "Active") return ("Activate enrollment", "retained");

        var requirements = new List<string>();
        if (paymentStatus != "Paid") requirements.Add("Complete payment");
        else if (!paymentClosedAtUtc.HasValue) requirements.Add("Close payment");
        if (!resultPublished) requirements.Add("Publish result");

        if (requirements.Count > 0) return (string.Join(" + ", requirements), "pending");
        return yearLevel == 4 && semester == "Semester 2"
            ? ("Ready to graduate", "ready")
            : ("Ready", "ready");
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;

    private sealed record StudentEnrollmentRow(
        Guid Id,
        Guid StudentId,
        string EnrollmentCode,
        string PublicId,
        string StudentCode,
        string FullName,
        string Email,
        string PhotoDataUrl,
        Guid DepartmentId,
        string? DepartmentName,
        int YearLevel,
        string Shift,
        string Status,
        string AcademicYear,
        string Semester,
        DateTime CreateAt);

    private sealed record PaymentState(string Status, DateTime? ClosedAtUtc);
}
