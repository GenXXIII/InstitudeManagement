using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed class StudentPaymentProgression(
    InstituteDbContext db,
    ActivePeriodLedgerCreator ledgerCreator)
{
    public async Task<string> ReleaseAsync(StudentPayment payment, CancellationToken cancellationToken)
    {
        var periodValues = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && (setting.Key == "currentTerm" || setting.Key == "startsOn"))
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var currentYear = periodValues.GetValueOrDefault("academic-year:currentYear", payment.AcademicYear);
        var currentSemester = periodValues.GetValueOrDefault("semester:currentTerm", payment.Semester);
        if (payment.AcademicYear == currentYear && payment.Semester == currentSemester) return "Current period paid";
        if (!IsNextPeriod(payment.AcademicYear, payment.Semester, currentYear, currentSemester)) return "Payment recorded";

        var previousEnrollment = await db.StudentEnrollments.AsNoTracking()
            .SingleAsync(enrollment => enrollment.Id == payment.StudentEnrollmentId, cancellationToken);
        var student = await db.Students
            .Include(item => item.Department)
            .SingleAsync(item => item.Id == payment.StudentId, cancellationToken);
        if (student.Status == "Inactive") return "Student inactive";
        if (await db.StudentEnrollments.AnyAsync(enrollment =>
                enrollment.StudentId == student.Id
                && enrollment.AcademicYear == currentYear
                && enrollment.Semester == currentSemester,
            cancellationToken))
        {
            return "Already advanced";
        }

        var academicYearChanged = payment.AcademicYear != currentYear;
        if (academicYearChanged && student.YearLevel >= 4)
        {
            student.Status = "Inactive";
            student.UpdatedAtUtc = DateTime.UtcNow;
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = student.Id,
                Type = "Student",
                Subject = student.FullName,
                Action = "Graduated",
                Details = JsonSerializer.Serialize(new
                {
                    student.StudentCode,
                    graduationAcademicYear = payment.AcademicYear,
                    payment.PaymentCode,
                    paymentStatus = payment.Status,
                    archive = "Management, Enrollment, Finance, Operation, and Record rows remain available in History."
                })
            });
            return "Graduated";
        }

        if (academicYearChanged)
        {
            student.YearLevel++;
            student.UpdatedAtUtc = DateTime.UtcNow;
        }

        var nextEnrollment = new StudentEnrollment
        {
            EnrollmentCode = previousEnrollment.EnrollmentCode,
            StudentId = student.Id,
            DepartmentId = previousEnrollment.DepartmentId,
            YearLevel = student.YearLevel,
            Shift = previousEnrollment.Shift,
            AcademicYear = currentYear,
            Semester = currentSemester,
            Status = "Active"
        };
        db.StudentEnrollments.Add(nextEnrollment);
        db.AuditLogs.Add(new AuditLog
        {
            ResourceId = student.Id,
            Type = "Finance",
            Subject = student.FullName,
            Action = "Payment hold released",
            Details = $"{payment.PaymentCode} was confirmed by the student; enrollment advanced to {currentYear} / {currentSemester}."
        });

        var start = DateOnly.TryParse(periodValues.GetValueOrDefault("semester:startsOn"), out var configuredStart)
            ? configuredStart
            : DateOnly.FromDateTime(DateTime.UtcNow);
        await ledgerCreator.CreateAsync(currentYear, currentSemester, start, cancellationToken);
        return "Advanced";
    }

    private static bool IsNextPeriod(
        string previousYear,
        string previousSemester,
        string currentYear,
        string currentSemester)
    {
        if (previousYear == currentYear)
        {
            return previousSemester == "Semester 1" && currentSemester == "Semester 2"
                || previousSemester == "Semester 2" && currentSemester == "Summer Term";
        }

        return FirstYear(currentYear) == FirstYear(previousYear) + 1
            && currentSemester == "Semester 1"
            && previousSemester is "Semester 2" or "Summer Term";
    }

    private static int FirstYear(string value)
    {
        var digits = new string(value.TakeWhile(character => !char.IsDigit(character)).Any()
            ? value.SkipWhile(character => !char.IsDigit(character)).TakeWhile(char.IsDigit).ToArray()
            : value.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var year) ? year : -1;
    }
}
