using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class GradeHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Grade";

    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "academic-year" && item.Key == "currentYear" || item.Section == "semester" && item.Key == "currentTerm")
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        var currentAcademicYear = period.GetValueOrDefault("academic-year:currentYear", string.Empty);
        var currentTerm = period.GetValueOrDefault("semester:currentTerm", string.Empty);
        var publishedPeriods = (await db.SemesterResultPublications.AsNoTracking()
                .Select(item => new { item.StudentId, item.AcademicYear, item.Term })
                .ToListAsync(cancellationToken))
            .Select(item => (item.StudentId, item.AcademicYear, item.Term))
            .ToHashSet();
        var closedPaymentPeriods = (await db.FinancialAccounts.AsNoTracking()
                .Where(item => item.ClosedAtUtc.HasValue)
                .Select(item => new { item.StudentId, item.AcademicYear, item.Semester })
                .ToListAsync(cancellationToken))
            .Select(item => (item.StudentId, item.AcademicYear, item.Semester))
            .ToHashSet();
        var grades = await db.GradeRecords.AsNoTracking()
            .Include(item => item.Student).ThenInclude(student => student!.Department)
            .Include(item => item.Course)
            .Include(item => item.SubmittedByTeacher)
            .ToListAsync(cancellationToken);
        grades = grades.Where(grade =>
                (grade.AcademicYear != currentAcademicYear || grade.Term != currentTerm)
                && publishedPeriods.Contains((grade.StudentId, grade.AcademicYear, grade.Term))
                && closedPaymentPeriods.Contains((grade.StudentId, grade.AcademicYear, grade.Term)))
            .ToList();
        var enrollments = await db.StudentEnrollments.AsNoTracking().ToListAsync(cancellationToken);
        var enrollmentByPeriod = enrollments
            .GroupBy(item => (item.StudentId, item.AcademicYear, item.Semester))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.CreateAt).First());

        return grades.Select(grade =>
        {
            enrollmentByPeriod.TryGetValue((grade.StudentId, grade.AcademicYear, grade.Term), out var enrollment);
            return Create(grade.Id, grade.ReviewedAtUtc ?? grade.SubmittedAtUtc ?? grade.UpdatedAtUtc, Type, grade.Student?.FullName ?? grade.GradeCode, "Recorded", new
            {
                grade.GradeCode,
                grade.StudentId,
                student = grade.Student?.FullName,
                studentCode = grade.Student?.StudentCode,
                grade.CourseId,
                courseCode = grade.Course?.CourseCode,
                course = grade.Course?.Name,
                departmentId = grade.Student?.DepartmentId,
                departmentCode = grade.Student?.Department?.DepartmentCode,
                department = grade.Student?.Department?.Name,
                yearLevel = enrollment?.YearLevel ?? grade.Student?.YearLevel,
                shift = enrollment?.Shift ?? grade.Student?.Shift,
                grade.AcademicYear,
                grade.Term,
                grade.AttendanceScore,
                grade.AttendanceMaximum,
                grade.AssignmentScore,
                grade.AssignmentMaximum,
                grade.MidtermScore,
                grade.MidtermMaximum,
                grade.FinalExamScore,
                grade.FinalExamMaximum,
                grade.Score,
                grade = grade.LetterGrade,
                grade.SubmittedByTeacherId,
                submittedByTeacher = grade.SubmittedByTeacher?.FullName,
                grade.ReviewStatus,
                grade.ReviewNote,
                grade.SubmissionVersion,
                grade.SubmittedAtUtc,
                grade.ReviewedAtUtc,
                grade.CreateAt,
                grade.UpdatedAtUtc,
            });
        }).ToList();
    }
}
