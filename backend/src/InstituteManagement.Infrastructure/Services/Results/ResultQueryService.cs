using System.Text.Json;
using InstituteManagement.Application.Features.Results;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Grades;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Results;

public sealed class ResultQueryService(InstituteDbContext db, FinancialProgression progression) : IResultQueryService
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
        var enrollments = await db.StudentEnrollments.AsNoTracking()
            .Include(item => item.Department)
            .Where(item => studentIds.Contains(item.StudentId) && item.Status != "Removed")
            .ToListAsync(cancellationToken);
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var allGrades = await db.GradeRecords.AsNoTracking().Include(item => item.Course).Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var finalizedGrades = allGrades.Where(item => item.FinalizedAtUtc.HasValue && (item.ReviewStatus == "Approved" || item.SubmittedByTeacherId == null)).ToList();
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Include(item => item.Course)
            .Where(item => item.Status != "Removed")
            .ToListAsync(cancellationToken);
        var timetableAssignments = await db.TimetableEnrollments.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.ScheduleEntry)
            .Where(item => item.Status == "Active")
            .ToListAsync(cancellationToken);
        var catalogCourses = await db.Courses.AsNoTracking().Where(item => item.IsActive).ToListAsync(cancellationToken);
        var publications = await db.SemesterResultPublications.AsNoTracking().Where(item => studentIds.Contains(item.StudentId)).ToListAsync(cancellationToken);
        var sessionRows = await db.ClassSessionRecords.AsNoTracking()
            .Where(item => (!departmentId.HasValue || item.DepartmentId == departmentId) && (!year.HasValue || item.YearLevel == year))
            .Select(item => new { item.AcademicYear, item.Term, item.StudentAttendanceJson })
            .ToListAsync(cancellationToken);
        var sessionAttendance = sessionRows.SelectMany(item => Deserialize(item.StudentAttendanceJson).Select(student => new SessionAttendance(student.StudentId, item.AcademicYear, item.Term, student.Status))).Where(item => studentIds.Contains(item.StudentId)).ToList();
        var settings = await db.SystemSettings.AsNoTracking().Where(item => item.Section == "academic-year" || item.Section == "semester" || item.Section == "grade-rules" || item.Section == "attendance-rules").ToListAsync(cancellationToken);
        var currentAcademicYear = academicYear ?? settings.FirstOrDefault(item => item.Section == "academic-year" && item.Key == "currentYear")?.Value ?? "2026–2027";
        var currentSemester = semester ?? settings.FirstOrDefault(item => item.Section == "semester" && item.Key == "currentTerm")?.Value ?? "Semester 1";
        var gradeSettings = settings.Where(item => item.Section == "grade-rules").ToDictionary(item => item.Key, item => item.Value);
        var attendanceSettings = settings.Where(item => item.Section == "attendance-rules").ToDictionary(item => item.Key, item => item.Value);
        var thresholds = GradeThresholds.From(gradeSettings);
        var weights = GradeWeights.From(gradeSettings);
        var attendanceRules = AttendanceResultRules.From(attendanceSettings);
        var expectedCourseCount = ExpectedCourseCount(gradeSettings);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var results = new List<SemesterResultDto>();

        foreach (var student in students)
        {
            var periods = history || publishedOnly
                ? publications.Where(item => item.StudentId == student.Id).Select(item => new Period(item.AcademicYear, item.Term)).Distinct().ToList()
                : finalizedGrades.Where(item => item.StudentId == student.Id)
                    .GroupBy(item => new Period(item.AcademicYear, item.Term))
                    .Where(group => group.Select(item => item.CourseId).Distinct().Count() == expectedCourseCount
                        && publications.All(item => item.StudentId != student.Id || item.AcademicYear != group.Key.AcademicYear || item.Term != group.Key.Semester))
                    .Select(group => group.Key)
                    .Append(new Period(currentAcademicYear, currentSemester))
                    .Distinct()
                    .ToList();
            foreach (var period in periods.Where(item => (string.IsNullOrWhiteSpace(semester) || item.Semester.Equals(semester, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(academicYear) || item.AcademicYear.Equals(academicYear, StringComparison.OrdinalIgnoreCase))))
            {
                if (history && period.AcademicYear == currentAcademicYear && period.Semester == currentSemester) continue;
                var publication = publications.FirstOrDefault(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester);
                if ((history || publishedOnly) && publication is null) continue;
                var enrollment = enrollments
                    .Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester)
                    .OrderByDescending(item => item.Status == "Active")
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .FirstOrDefault();
                var resultDepartmentId = enrollment?.DepartmentId ?? student.DepartmentId ?? Guid.Empty;
                var resultDepartment = enrollment?.Department?.Name ?? student.Department?.Name ?? "Unassigned";
                var resultYear = enrollment?.YearLevel ?? student.YearLevel;
                var resultShift = enrollment?.Shift ?? student.Shift;
                var periodAttendance = attendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var timetableAttendance = sessionAttendance.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester).ToList();
                var submittedGrades = allGrades.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                var finalizedPeriodGrades = finalizedGrades.Where(item => item.StudentId == student.Id && item.AcademicYear == period.AcademicYear && item.Term == period.Semester).ToList();
                if (!history && !publishedOnly && finalizedPeriodGrades.Select(item => item.CourseId).Distinct().Count() != expectedCourseCount) continue;
                var cohortTimetableCourses = timetableAssignments.Where(item =>
                        item.AcademicYear == period.AcademicYear
                        && item.Semester == period.Semester
                        && item.YearLevel == resultYear
                        && item.ScheduleEntry?.Shift == resultShift
                        && item.Course?.DepartmentId == resultDepartmentId)
                    .Select(item => item.Course!)
                    .ToList();
                var fallbackAssignedCourses = cohortTimetableCourses.Count > 0
                    ? []
                    : courseAssignments.Where(item =>
                        item.AcademicYear == period.AcademicYear
                        && item.Semester == period.Semester
                        && item.DepartmentId == resultDepartmentId
                        && item.YearLevel == resultYear
                        && item.Course is not null).Select(item => item.Course!).ToList();
                var fallbackCatalogCourses = cohortTimetableCourses.Count > 0 || fallbackAssignedCourses.Count > 0
                    ? []
                    : catalogCourses.Where(item => item.DepartmentId == resultDepartmentId && item.YearLevel == resultYear && item.Semester == period.Semester).ToList();
                var learningCourses = submittedGrades.Where(item => item.Course is not null).Select(item => item.Course!)
                    .Concat(cohortTimetableCourses)
                    .Concat(fallbackAssignedCourses)
                    .Concat(fallbackCatalogCourses)
                    .GroupBy(item => item.Id)
                    .Select(item => item.First())
                    .OrderBy(item => item.CourseCode)
                    .Take(expectedCourseCount)
                    .ToList();
                var periodGrades = learningCourses.Select(course =>
                {
                    var finalized = finalizedPeriodGrades.FirstOrDefault(item => item.CourseId == course.Id);
                    return new CourseResultDto(course.Id, course.CourseCode, course.Name, finalized?.Score, finalized?.LetterGrade ?? "Draft", finalized is not null);
                }).ToList();
                var approvedResults = periodGrades.Where(item => item.IsApproved).ToList();
                var total = approvedResults.Sum(item => item.Score ?? 0m);
                var average = SemesterResultRules.Average(approvedResults.Select(item => item.Score ?? 0m), expectedCourseCount);
                var statuses = timetableAttendance.Count > 0 ? timetableAttendance.Select(item => item.Status).ToList() : periodAttendance.Select(item => item.Status).ToList();
                var absent = statuses.Count(item => item == "Absent");
                var permission = statuses.Count(item => item is "Excused" or "Permission");
                var attendanceScore = attendanceRules.Score(weights.Attendance, absent, permission);
                var attendanceGrade = attendanceRules.Grade(attendanceScore, weights.Attendance);
                var overallGrade = thresholds.Letter(average);
                var totalGrade = SemesterResultRules.Outcome(approvedResults.Select(item => item.Grade).ToList(), average, thresholds, expectedCourseCount, attendanceRules.Outcome(absent, permission));
                var publicationStatus = publication is not null ? "Declared" : approvedResults.Count == expectedCourseCount ? "Ready" : "Draft";
                results.Add(new SemesterResultDto(
                    student.Id, student.StudentCode,
                    !string.IsNullOrWhiteSpace(enrollment?.ResultCode)
                        ? enrollment.ResultCode
                        : codeFormat.LinkedWithConfiguredPrefix(student.StudentCode, "student", enrollment?.EnrollmentCode ?? string.Empty, "resultCodePrefix", "RES"),
                    student.FullName, resultDepartmentId, resultDepartment, resultYear, resultShift,
                    period.AcademicYear, period.Semester,
                    statuses.Count(item => item is "Present" or "Late"), absent,
                    permission, attendanceScore, weights.Attendance, attendanceGrade,
                    periodGrades, expectedCourseCount, approvedResults.Count, total, average, overallGrade, totalGrade,
                    publicationStatus, publication is not null, publication?.PublishedAtUtc));
            }
        }
        return results.OrderBy(item => item.Year).ThenBy(item => ShiftOrder(item.Shift)).ThenBy(item => item.FullName).ThenByDescending(item => item.AcademicYear).ThenBy(item => item.Semester).ToList();
    }

    public async Task PublishAsync(Guid studentId, string academicYear, string semester, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester)) throw new ArgumentException("Academic year and semester are required.");
        if (await db.SemesterResultPublications.AnyAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Term == semester, cancellationToken))
            throw new InvalidOperationException("This Semester Result is already declared.");
        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var approved = await db.GradeRecords.CountAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Term == semester && item.FinalizedAtUtc.HasValue && (item.ReviewStatus == "Approved" || item.SubmittedByTeacherId == null), cancellationToken);
        var expectedCourseCount = await ConfiguredExpectedCourseCountAsync(cancellationToken);
        if (approved != expectedCourseCount)
            throw new InvalidOperationException($"All {expectedCourseCount} course grades must be confirmed in Student Result before declaration.");
        var publication = new SemesterResultPublication { StudentId = studentId, AcademicYear = academicYear.Trim(), Term = semester.Trim(), PublishedAtUtc = DateTime.UtcNow };
        db.SemesterResultPublications.Add(publication);
        db.AuditLogs.Add(new AuditLog { ResourceId = publication.Id, Type = "Academic result", Subject = student.FullName, Action = "Declared", Details = $"{publication.AcademicYear} · {publication.Term} · read-only · eligible for semester progression" });
        await db.SaveChangesAsync(cancellationToken);
        await ReleaseProgressionAsync(studentId, publication.AcademicYear, publication.Term, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PublishAllAsync(Guid? departmentId, int? year, CancellationToken cancellationToken)
    {
        var ready = (await GetAsync(departmentId, year, null, null, false, cancellationToken))
            .Where(item => item.PublicationStatus == "Ready")
            .ToList();
        if (ready.Count == 0) return 0;

        var studentIds = ready.Select(item => item.StudentId).ToList();
        var students = await db.Students.AsNoTracking()
            .Where(item => studentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var result in ready)
        {
            var publication = new SemesterResultPublication
            {
                StudentId = result.StudentId,
                AcademicYear = result.AcademicYear,
                Term = result.Semester,
                PublishedAtUtc = DateTime.UtcNow
            };
            db.SemesterResultPublications.Add(publication);
            db.AuditLogs.Add(new AuditLog
            {
                ResourceId = publication.Id,
                Type = "Academic result",
                Subject = students.GetValueOrDefault(result.StudentId)?.FullName ?? result.FullName,
                Action = "Declared",
                Details = $"{result.ResultCode} · {result.AcademicYear} · {result.Semester} · read-only · eligible for semester progression"
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var result in ready)
            await ReleaseProgressionAsync(result.StudentId, result.AcademicYear, result.Semester, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ready.Count;
    }

    private async Task ReleaseProgressionAsync(Guid studentId, string academicYear, string term, CancellationToken cancellationToken)
    {
        var requirePaid = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "finance" && item.Key == "requirePaidForAdvancement")
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);
        var paymentRequired = !bool.TryParse(requirePaid, out var configured) || configured;
        var account = await db.FinancialAccounts
            .Include(item => item.StudentEnrollment)
            .Include(item => item.Student)
            .FirstOrDefaultAsync(item => item.StudentId == studentId && item.AcademicYear == academicYear && item.Semester == term, cancellationToken);
        if (account is null || paymentRequired && !account.ClosedAtUtc.HasValue) return;
        await progression.ReleaseAsync(account, cancellationToken);
    }

    private sealed record Period(string AcademicYear, string Semester);
    private sealed record SessionAttendance(Guid StudentId, string AcademicYear, string Semester, string Status);
    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private async Task<int> ConfiguredExpectedCourseCountAsync(CancellationToken cancellationToken)
    {
        var value = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "grade-rules" && item.Key == "expectedCourseCount")
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return int.TryParse(value, out var count) && count > 0 ? count : SemesterResultRules.ExpectedCourseCount;
    }

    private static int ExpectedCourseCount(IReadOnlyDictionary<string, string> settings) =>
        int.TryParse(settings.GetValueOrDefault("expectedCourseCount"), out var count) && count > 0
            ? count
            : SemesterResultRules.ExpectedCourseCount;

    private static int ShiftOrder(string value) => value.Trim().ToLowerInvariant() switch
    {
        "morning" => 0,
        "afternoon" => 1,
        "evening" => 2,
        "weekend" => 3,
        _ => 4
    };
}
