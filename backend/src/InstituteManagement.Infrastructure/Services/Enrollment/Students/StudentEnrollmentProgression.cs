using System.Text.Json;
using InstituteManagement.Application.Features.Enrollment.Students.Progression;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

public sealed class StudentEnrollmentProgression(
    InstituteDbContext db,
    ActivePeriodLedgerCreator ledgerCreator) : IStudentEnrollmentProgression
{
    public async Task<string> ReleaseAsync(
        StudentEnrollmentProgressionRequest request,
        CancellationToken cancellationToken)
    {
        var periodValues = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && (setting.Key == "currentTerm" || setting.Key == "startsOn"))
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var currentYear = periodValues.GetValueOrDefault("academic-year:currentYear", request.AcademicYear);
        var currentSemester = periodValues.GetValueOrDefault("semester:currentTerm", request.Semester);
        if (request.AcademicYear == currentYear && request.Semester == currentSemester) return "Current period paid";
        var publicationIsPending = db.SemesterResultPublications.Local.Any(item =>
            item.StudentId == request.StudentId
            && item.AcademicYear == request.AcademicYear
            && item.Term == request.Semester
            && db.Entry(item).State != EntityState.Deleted);
        if (!publicationIsPending && !await db.SemesterResultPublications.AsNoTracking().AnyAsync(item =>
                item.StudentId == request.StudentId
                && item.AcademicYear == request.AcademicYear
                && item.Term == request.Semester,
            cancellationToken))
            return "Waiting for Semester Result declaration";
        if (!IsNextPeriod(request.AcademicYear, request.Semester, currentYear, currentSemester)) return "Payment recorded";

        var previousEnrollment = await db.StudentEnrollments.AsNoTracking()
            .SingleAsync(enrollment => enrollment.Id == request.StudentEnrollmentId, cancellationToken);
        var student = await db.Students
            .Include(item => item.Department)
            .SingleAsync(item => item.Id == request.StudentId, cancellationToken);
        if (student.Status == "Inactive") return "Student inactive";
        if (await db.StudentEnrollments.AnyAsync(enrollment =>
                enrollment.StudentId == student.Id
                && enrollment.AcademicYear == currentYear
                && enrollment.Semester == currentSemester,
            cancellationToken))
        {
            return "Already advanced";
        }

        var academicYearChanged = request.AcademicYear != currentYear;
        if (academicYearChanged && student.YearLevel >= 4)
        {
            var completedAtUtc = DateTime.UtcNow;
            var completedAcademicEnrollment = await StudentAcademicEnrollmentResolver.GetOrCreateAsync(
                db,
                student.Id,
                previousEnrollment.EnrollmentCode,
                cancellationToken);
            StudentAcademicEnrollmentResolver.Complete(completedAcademicEnrollment, completedAtUtc);
            student.Status = "Inactive";
            student.UpdatedAtUtc = completedAtUtc;
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = student.Id,
                Type = "Student",
                Subject = student.FullName,
                Action = "Graduated",
                Details = JsonSerializer.Serialize(new
                {
                    student.StudentCode,
                    graduationAcademicYear = request.AcademicYear,
                    request.FinancialAccountCode,
                    financeStatus = request.FinanceStatus,
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

        var periodCodes = await BusinessCodeFormatter.GenerateStudentPeriodWorkflowAsync(
            db,
            student.StudentCode,
            previousEnrollment.EnrollmentCode,
            student.YearLevel,
            currentSemester,
            cancellationToken);
        var academicEnrollment = await StudentAcademicEnrollmentResolver.GetOrCreateAsync(
            db,
            student.Id,
            periodCodes.Enrollment,
            cancellationToken);
        var enrollmentId = Guid.NewGuid();
        var nextEnrollment = new StudentEnrollment
        {
            Id = enrollmentId,
            StudentAcademicEnrollmentId = academicEnrollment.Id,
            StudentAcademicEnrollment = academicEnrollment,
            EnrollmentCode = periodCodes.Enrollment,
            PublicId = !string.IsNullOrWhiteSpace(previousEnrollment.PublicId)
                ? previousEnrollment.PublicId
                : await BusinessCodeFormatter.GenerateEnrollmentPublicIdAsync(db, previousEnrollment.Id, "studentPublicIdPrefix", "STU", cancellationToken),
            FinanceCode = await BusinessCodeFormatter.GenerateStudentPeriodScopedAsync(db, previousEnrollment.EnrollmentCode, student.YearLevel, currentSemester, "financeCodePrefix", "FIN", cancellationToken),
            ResultCode = await BusinessCodeFormatter.GenerateStudentPeriodScopedAsync(db, previousEnrollment.EnrollmentCode, student.YearLevel, currentSemester, "resultCodePrefix", "RES", cancellationToken),
            OperationCode = periodCodes.Operation,
            RecordCode = periodCodes.Record,
            HistoryCode = periodCodes.History,
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
            Details = JsonSerializer.Serialize(new
            {
                request.FinancialAccountCode,
                previousEnrollment.EnrollmentCode,
                financeStatus = request.FinanceStatus,
                enrollmentEffect = $"Advanced to {currentYear} / {currentSemester}"
            })
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
