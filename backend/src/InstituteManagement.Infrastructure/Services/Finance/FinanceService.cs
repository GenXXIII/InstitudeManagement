using System.Globalization;
using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed class FinanceService(
    InstituteDbContext db,
    InstituteCache cache,
    StudentPaymentSynchronizer synchronizer,
    StudentPaymentProgression progression) : IFinanceService
{
    public async Task<IReadOnlyList<StudentPaymentDto>> GetAsync(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        CancellationToken cancellationToken)
    {
        await EnsureLedgerAsync(cancellationToken);
        var payments = await PaymentQuery()
            .Where(payment =>
                (string.IsNullOrWhiteSpace(academicYear) || payment.AcademicYear == academicYear)
                && (string.IsNullOrWhiteSpace(semester) || payment.Semester == semester)
                && (string.IsNullOrWhiteSpace(status) || status == "All" || payment.Status == status))
            .OrderBy(payment => payment.Status == "Paid")
            .ThenBy(payment => payment.DueOn)
            .ThenBy(payment => payment.Student!.FullName)
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            payments = payments.Where(payment => Matches(
                search,
                payment.PaymentCode,
                payment.Student!.StudentCode,
                payment.Student.PublicId,
                payment.Student.FullName,
                payment.StudentEnrollment!.EnrollmentCode,
                payment.StudentEnrollment.Department!.Name)).ToList();
        }
        return await MapAsync(payments, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentPaymentDto>> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (!await db.Students.AsNoTracking().AnyAsync(student => student.Id == studentId && student.Status != "Inactive", cancellationToken))
            throw new KeyNotFoundException("Active student not found.");
        await EnsureLedgerAsync(cancellationToken);
        var payments = await PaymentQuery()
            .Where(payment => payment.StudentId == studentId)
            .OrderBy(payment => payment.Status == "Paid")
            .ThenByDescending(payment => payment.AcademicYear)
            .ThenByDescending(payment => payment.Semester)
            .ToListAsync(cancellationToken);
        return await MapAsync(payments, cancellationToken);
    }

    public async Task<StudentPaymentDto> ConfirmAsync(
        Guid studentId,
        Guid paymentId,
        StudentPaymentConfirmationDto request,
        CancellationToken cancellationToken)
    {
        var payment = await PaymentQuery().SingleOrDefaultAsync(item => item.Id == paymentId && item.StudentId == studentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student payment not found.");
        if (request.QrPayload?.Trim() != QrPayload(payment))
            throw new ArgumentException("The scanned QR code does not match this student payment.");

        if (payment.Status != "Paid")
        {
            var confirmedAt = DateTime.UtcNow;
            payment.Status = "Paid";
            payment.ConfirmationMethod = "Student QR scan";
            payment.PaidAtUtc = confirmedAt;
            payment.ReminderReadAtUtc = confirmedAt;
            payment.UpdatedAtUtc = confirmedAt;
            var result = await progression.ReleaseAsync(payment, cancellationToken);
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = payment.StudentId,
                Type = "Finance",
                Subject = payment.Student!.FullName,
                Action = "Payment confirmed",
                Details = $"{payment.PaymentCode} / {payment.AcademicYear} / {payment.Semester} / {payment.AmountDue.ToString("0.00", CultureInfo.InvariantCulture)} {payment.Currency} / {payment.ConfirmationMethod} / {result}."
            });
            await SaveAsync(cancellationToken);
        }

        return AssertSingle(await MapAsync([payment], cancellationToken));
    }

    public async Task<StudentPaymentDto> MarkReminderReadAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await PaymentQuery().SingleOrDefaultAsync(item => item.Id == paymentId && item.StudentId == studentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student payment not found.");
        if (payment.ReminderSentAtUtc.HasValue && !payment.ReminderReadAtUtc.HasValue)
        {
            payment.ReminderReadAtUtc = DateTime.UtcNow;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            await SaveAsync(cancellationToken);
        }
        return AssertSingle(await MapAsync([payment], cancellationToken));
    }

    private IQueryable<StudentPayment> PaymentQuery() => db.StudentPayments
        .Include(payment => payment.Student)
        .Include(payment => payment.StudentEnrollment)!
            .ThenInclude(enrollment => enrollment!.Department);

    private async Task<IReadOnlyList<StudentPaymentDto>> MapAsync(
        IReadOnlyCollection<StudentPayment> payments,
        CancellationToken cancellationToken)
    {
        if (payments.Count == 0) return [];
        var periodValues = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && setting.Key == "currentTerm")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var currentYear = periodValues.GetValueOrDefault("academic-year:currentYear", "");
        var currentSemester = periodValues.GetValueOrDefault("semester:currentTerm", "");
        var years = payments.Select(payment => payment.AcademicYear).Distinct().ToList();
        var semesters = payments.Select(payment => payment.Semester).Distinct().ToList();
        var timetableRows = await db.TimetableEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.Course)
            .Include(enrollment => enrollment.ScheduleEntry)
            .Where(enrollment =>
                years.Contains(enrollment.AcademicYear)
                && semesters.Contains(enrollment.Semester)
                && enrollment.Status == "Active"
                && enrollment.Course != null
                && enrollment.ScheduleEntry != null
                && enrollment.ScheduleEntry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return payments.Select(payment =>
        {
            var enrollment = payment.StudentEnrollment!;
            var timetableReady = timetableRows.Any(timetable =>
                timetable.AcademicYear == payment.AcademicYear
                && timetable.Semester == payment.Semester
                && timetable.YearLevel == enrollment.YearLevel
                && timetable.Course!.DepartmentId == enrollment.DepartmentId
                && timetable.ScheduleEntry!.Shift == enrollment.Shift);
            return new StudentPaymentDto(
                payment.Id,
                payment.PaymentCode,
                payment.StudentId,
                payment.Student!.StudentCode,
                payment.Student.PublicId,
                payment.Student.FullName,
                enrollment.Department?.Name ?? "Unassigned",
                enrollment.EnrollmentCode,
                enrollment.YearLevel,
                enrollment.Shift,
                payment.AcademicYear,
                payment.Semester,
                payment.AmountDue,
                payment.Currency,
                payment.DueOn,
                payment.Status,
                payment.ConfirmationMethod,
                payment.PaidAtUtc,
                payment.ReminderSentAtUtc,
                payment.ReminderReadAtUtc,
                timetableReady ? "Ready" : "Waiting",
                payment.AcademicYear == currentYear && payment.Semester == currentSemester ? "Current" : "Retained",
                QrPayload(payment),
                payment.CreateAt);
        }).ToList();
    }

    private async Task EnsureLedgerAsync(CancellationToken cancellationToken)
    {
        await synchronizer.EnsureAllAsync(cancellationToken);
        if (db.ChangeTracker.HasChanges()) await SaveAsync(cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private static string QrPayload(StudentPayment payment) =>
        $"INK-PAY|{payment.Id:N}|{payment.StudentId:N}|{payment.AmountDue.ToString("0.00", CultureInfo.InvariantCulture)}|{payment.Currency}";

    private static bool Matches(string search, params string[] values) =>
        values.Any(value => value.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

    private static StudentPaymentDto AssertSingle(IReadOnlyList<StudentPaymentDto> payments) =>
        payments.Single();
}
