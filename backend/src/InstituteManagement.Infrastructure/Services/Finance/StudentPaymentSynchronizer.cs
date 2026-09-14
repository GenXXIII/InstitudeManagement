using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed class StudentPaymentSynchronizer(
    InstituteDbContext db,
    FinanceSettingsReader settingsReader)
{
    public async Task<int> EnsureAllAsync(CancellationToken cancellationToken)
    {
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment => enrollment.Status != "Removed" && enrollment.Student != null)
            .OrderBy(enrollment => enrollment.CreateAt)
            .ToListAsync(cancellationToken);
        return await EnsureAsync(enrollments, cancellationToken);
    }

    public async Task<int> EnsurePeriodAsync(
        string academicYear,
        string semester,
        CancellationToken cancellationToken)
    {
        var enrollments = await db.StudentEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Student)
            .Where(enrollment =>
                enrollment.AcademicYear == academicYear
                && enrollment.Semester == semester
                && enrollment.Status == "Active"
                && enrollment.Student != null)
            .OrderBy(enrollment => enrollment.StudentId)
            .ToListAsync(cancellationToken);
        return await EnsureAsync(enrollments, cancellationToken);
    }

    public async Task<StudentPayment> EnsureForEnrollmentAsync(
        StudentEnrollment enrollment,
        Student student,
        CancellationToken cancellationToken)
    {
        var local = db.StudentPayments.Local.FirstOrDefault(payment => payment.StudentEnrollmentId == enrollment.Id);
        var existing = local ?? await db.StudentPayments.FirstOrDefaultAsync(
            payment => payment.StudentEnrollmentId == enrollment.Id,
            cancellationToken);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (existing is not null)
        {
            ApplyPendingPrice(existing, settings);
            return existing;
        }

        var payment = Create(enrollment, student, settings);
        db.StudentPayments.Add(payment);
        return payment;
    }

    private async Task<int> EnsureAsync(
        IReadOnlyCollection<StudentEnrollment> enrollments,
        CancellationToken cancellationToken)
    {
        if (enrollments.Count == 0) return 0;
        var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();
        var existing = (await db.StudentPayments
                .Where(payment => enrollmentIds.Contains(payment.StudentEnrollmentId))
                .ToListAsync(cancellationToken))
            .ToDictionary(payment => payment.StudentEnrollmentId);
        foreach (var local in db.StudentPayments.Local.Where(payment => enrollmentIds.Contains(payment.StudentEnrollmentId)))
        {
            existing[local.StudentEnrollmentId] = local;
        }

        var settings = await settingsReader.GetAsync(cancellationToken);
        var created = 0;
        foreach (var enrollment in enrollments)
        {
            if (existing.TryGetValue(enrollment.Id, out var payment))
            {
                ApplyPendingPrice(payment, settings);
                continue;
            }

            db.StudentPayments.Add(Create(enrollment, enrollment.Student!, settings));
            created++;
        }
        return created;
    }

    private static StudentPayment Create(
        StudentEnrollment enrollment,
        Student student,
        FinanceSettings settings)
    {
        var free = settings.SemesterPrice <= 0;
        return new StudentPayment
        {
            PaymentCode = PaymentCode(student.StudentCode),
            StudentEnrollmentId = enrollment.Id,
            StudentId = enrollment.StudentId,
            AcademicYear = enrollment.AcademicYear,
            Semester = enrollment.Semester,
            AmountDue = settings.SemesterPrice,
            Currency = settings.Currency,
            DueOn = DateOnly.FromDateTime(enrollment.CreateAt).AddDays(settings.PaymentDueDays),
            Status = free ? "Paid" : "Pending",
            ConfirmationMethod = free ? "No payment required" : string.Empty,
            PaidAtUtc = free ? DateTime.UtcNow : null
        };
    }

    private static void ApplyPendingPrice(StudentPayment payment, FinanceSettings settings)
    {
        if (payment.Status != "Pending") return;
        payment.AmountDue = settings.SemesterPrice;
        payment.Currency = settings.Currency;
        if (settings.SemesterPrice > 0) return;
        payment.Status = "Paid";
        payment.ConfirmationMethod = "No payment required";
        payment.PaidAtUtc = DateTime.UtcNow;
        payment.ReminderReadAtUtc = DateTime.UtcNow;
        payment.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string PaymentCode(string studentCode)
    {
        var safeCode = studentCode.Trim();
        if (safeCode.Length > 52) safeCode = safeCode[..52];
        return $"PAY-{safeCode}";
    }
}
