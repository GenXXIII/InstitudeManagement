using System.Text.Json;
using InstituteManagement.Application.Features.Results;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Results;

public sealed class ResultQueryService(InstituteDbContext db) : IResultQueryService
{
    public Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, CancellationToken cancellationToken) =>
        GetAsync(departmentId, year, semester, academicYear, history, null, false, cancellationToken);

    public async Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, CancellationToken cancellationToken)
    {
        var studentQuery = db.Students.AsNoTracking().Include(item => item.Department)
            .Where(item => (!departmentId.HasValue || item.DepartmentId == departmentId) && (!year.HasValue || item.YearLevel == year) && (!studentId.HasValue || item.Id == studentId));
        if (!history && !publishedOnly) studentQuery = studentQuery.Where(item => item.Status != "Inactive");
        var students = await studentQuery.ToListAsync(cancellationToken);
        var studentIds = students.Select(item => item.Id).ToHashSet();
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var allGrades = await db.GradeRecords.AsNoTracking().Include(item => item.Course).Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var approvedGrades = allGrades.Where(item => item.ReviewStatus == "Approved" || item.SubmittedByTeacherId == null).ToList();
        var publications = await db.SemesterResultPublications.AsNoTracking().Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var sessionRows = await db.ClassSessionRecords.AsNoTracking()
            .Where(item => (!departmentId.HasValue || item.DepartmentId == departmentId) && (!year.HasValue || item.YearLevel == year))
            .Select(item => new { item.AcademicYear, item.Term, item.StudentAttendanceJson })
            .ToListAsync(cancellationToken);
        var sessionAttendance = sessionRows.SelectMany(item => Deserialize(item.StudentAttendanceJson).Select(student => new SessionAttendance(student.StudentId, item.AcademicYear, item.Term, student.Status))).Where(item => studentIds.Contains(item.StudentId)).ToList();
        var settings = await db.SystemSettings.AsNoTracking().Where(item => item.Section == "academic-year" || item.Section == "semester" || item.Section == "grade-rules" || item.Section == "attendance-rules").ToListAsync(cancellationToken);
        var currentAcademicYear = academicYear ?? settings.FirstOrDefault(item => item.Section == "academic-year" && item.Key == "currentYear")?.Value ?? "2026–2027";
        var currentSemester = semester ?? settings.FirstOrDefault(item => item.Section == "semester" && item.Key == "currentTerm")?.Value ?? "Semester 1";
        var thresholds = GradeThresholds.From(settings.Where(item => item.Section == "grade-rules").ToDictionary(item => item.Key, item => item.Value));
        var autoPercentageValue = settings.FirstOrDefault(item => item.Section == "attendance-rules" && item.Key == "autoPercentage")?.Value;
        var autoPercentage = !bool.TryParse(autoPercentageValue, out var calculatePercentage) || calculatePercentage;
        var results = new List<SemesterResultDto>();

        foreach (var student in students)
        {
            var periods = history || publishedOnly
                ? publications.Where(item => item.StudentId == student.Id).Select(item => new Period(item.AcademicYear, item.Term)).Distinct().ToList()
                : [new Period(currentAcademicYear, currentSemester)];
            foreach (var period in periods.Where(item => (string.IsNullOrWhiteSpace(semester) || item.Semester.Equals(semester, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(academicYear) || item.AcademicYear.Equals(academicYear, StringComparison.OrdinalIgnoreCase))))
            {
                var publication = publications.FirstOrDefault(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester);
                if ((history || publishedOnly) && publication is null) continue;
                var periodAttendance = attendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var timetableAttendance = sessionAttendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester).ToList();
                var periodGrades = approvedGrades.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester)
                    .OrderBy(item => item.Course!.CourseCode)
                    .Select(item => new CourseResultDto(item.CourseId, item.Course?.CourseCode ?? "—", item.Course?.Name ?? "Course", item.Score, item.LetterGrade))
                    .Take(SemesterResultRules.ExpectedCourseCount).ToList();
                var total = periodGrades.Sum(item => item.Score);
                var average = SemesterResultRules.Average(periodGrades.Select(item => item.Score));
                var statuses = timetableAttendance.Count > 0 ? timetableAttendance.Select(item => item.Status).ToList() : periodAttendance.Select(item => item.Status).ToList();
                var absent = statuses.Count(item => item == "Absent");
                var totalGrade = SemesterResultRules.Outcome(absent, periodGrades.Select(item => item.Grade).ToList(), average, thresholds, autoPercentage);
                var publicationStatus = publication is not null ? "Published" : periodGrades.Count == SemesterResultRules.ExpectedCourseCount ? "Ready" : "Draft";
                results.Add(new SemesterResultDto(
                    student.Id, student.StudentCode, student.FullName, student.DepartmentId ?? Guid.Empty, student.Department?.Name ?? "Unassigned", student.YearLevel,
                    period.AcademicYear, period.Semester,
                    statuses.Count(item => item is "Present" or "Late"), absent,
                    statuses.Count(item => item is "Excused" or "Permission"),
                    periodGrades, periodGrades.Count, total, average, totalGrade,
                    publicationStatus, publication is not null, publication?.PublishedAtUtc));
            }
        }
        return results.OrderBy(item => item.Year).ThenBy(item => item.FullName).ThenByDescending(item => item.AcademicYear).ThenBy(item => item.Semester).ToList();
    }

    public async Task PublishAsync(Guid studentId, string academicYear, string semester, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester)) throw new ArgumentException("Academic year and semester are required.");
        if (await db.SemesterResultPublications.AnyAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Term == semester, cancellationToken))
            throw new InvalidOperationException("This semester result is already published.");
        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var approved = await db.GradeRecords.CountAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Term == semester && (item.ReviewStatus == "Approved" || item.SubmittedByTeacherId == null), cancellationToken);
        if (approved != SemesterResultRules.ExpectedCourseCount)
            throw new InvalidOperationException($"All {SemesterResultRules.ExpectedCourseCount} course grades must be approved before publishing.");
        var publication = new SemesterResultPublication { StudentId = studentId, AcademicYear = academicYear.Trim(), Term = semester.Trim(), PublishedAtUtc = DateTime.UtcNow };
        db.SemesterResultPublications.Add(publication);
        db.AuditLogs.Add(new AuditLog { ResourceId = publication.Id, Type = "Academic result", Subject = student.FullName, Action = "Published", Details = $"{publication.AcademicYear} · {publication.Term} · visible to Student" });
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record Period(string AcademicYear, string Semester);
    private sealed record SessionAttendance(Guid StudentId, string AcademicYear, string Semester, string Status);
    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
