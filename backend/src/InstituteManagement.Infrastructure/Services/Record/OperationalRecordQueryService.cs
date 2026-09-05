using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Grades;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class OperationalRecordQueryService(IEnumerable<IOperationalRecordReader> readers, InstituteDbContext db) : IOperationalRecordQueryService
{
    private readonly IReadOnlyDictionary<string, IOperationalRecordReader> _readers = readers.ToDictionary(x => x.Module, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, bool history, CancellationToken cancellationToken)
    {
        if (!_readers.TryGetValue(module, out var reader)) throw new ArgumentException($"Operational records for '{module}' are not supported.");
        var records = await reader.GetAsync(departmentId, cancellationToken);
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Section == "academic-year" || x.Section == "semester" || x.Section == "grade-rules" || x.Section == "attendance-rules")
            .ToListAsync(cancellationToken);
        var academicYear = settings.FirstOrDefault(x => x.Section == "academic-year" && x.Key == "currentYear")?.Value ?? "2026\u20132027";
        var term = settings.FirstOrDefault(x => x.Section == "semester" && x.Key == "currentTerm")?.Value ?? "Semester 1";
        var thresholds = GradeThresholds.From(settings.Where(x => x.Section == "grade-rules").ToDictionary(x => x.Key, x => x.Value));
        var autoPercentageValue = settings.FirstOrDefault(x => x.Section == "attendance-rules" && x.Key == "autoPercentage")?.Value;
        var applyAttendanceRules = !bool.TryParse(autoPercentageValue, out var configuredAutoPercentage) || configuredAutoPercentage;

        var graduatedStudentIds = (await db.AuditLogs.AsNoTracking()
            .Where(log => log.Type == "Student" && log.Action == "Graduated" && log.ResourceId.HasValue)
            .Select(log => log.ResourceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
        var isStudentModule = module.Equals("students", StringComparison.OrdinalIgnoreCase);
        if (isStudentModule)
            records = records.Where(record => history ? graduatedStudentIds.Contains(record.ResourceId) : !graduatedStudentIds.Contains(record.ResourceId)).ToList();

        records = (isStudentModule
                ? history ? CollapseGraduatedStudentHistory(records) : SplitByPeriod(records, academicYear, term, PeriodScope.All)
                : SplitByPeriod(records, academicYear, term, history ? PeriodScope.Closed : PeriodScope.Current))
            .Select(record => record.Module == "Student" ? AddStudentInsights(record, thresholds, applyAttendanceRules, academicYear, term, history) : record)
            .ToList();
        if (string.IsNullOrWhiteSpace(search)) return records;
        var searchTerm = search.Trim();
        return records.Where(x => Matches(searchTerm, x.Subject, x.Code, x.Department, x.Identifier, x.Summary, x.AcademicYear, x.Term)
            || x.Activities.Any(activity => activity.Values.Any(value => value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))).ToList();
    }

    private static IReadOnlyList<OperationalRecordDto> SplitByPeriod(IReadOnlyList<OperationalRecordDto> records, string academicYear, string term, PeriodScope scope)
    {
        var periodRecords = new List<OperationalRecordDto>();
        foreach (var record in records)
        {
            var groups = record.Activities
                .Where(activity => !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Academic year")) && !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Term")))
                .GroupBy(activity => new { AcademicYear = activity["Academic year"], Term = activity["Term"] })
                .Where(group => scope == PeriodScope.All
                    || scope == PeriodScope.Current && group.Key.AcademicYear == academicYear && group.Key.Term == term
                    || scope == PeriodScope.Closed && (group.Key.AcademicYear != academicYear || group.Key.Term != term));
            foreach (var group in groups)
            {
                var activities = group.ToList();
                periodRecords.Add(record with
                {
                    Id = record.Module == "Session" ? record.Id : PeriodId(record.Id, group.Key.AcademicYear, group.Key.Term),
                    Activities = activities,
                    Summary = $"{activities.Count:N0} recorded activities",
                    Status = group.Key.AcademicYear == academicYear && group.Key.Term == term
                        ? record.Module == "Teacher" ? activities.Select(activity => activity.GetValueOrDefault("Teacher attendance")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? record.Status : record.Status
                        : "Closed",
                    AcademicYear = group.Key.AcademicYear,
                    Term = group.Key.Term,
                    Code = activities.Select(activity => activity.GetValueOrDefault("Enrollment code")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? record.Code,
                    Insights = null
                });
            }
        }
        return periodRecords;
    }

    private static IReadOnlyList<OperationalRecordDto> CollapseGraduatedStudentHistory(IReadOnlyList<OperationalRecordDto> records) => records
        .Select(record =>
        {
            var activities = record.Activities
                .Where(activity => !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Academic year")) && !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Term")))
                .OrderByDescending(activity => activity.GetValueOrDefault("Academic year"))
                .ThenByDescending(activity => activity.GetValueOrDefault("Term"))
                .ToList();
            var periods = activities
                .Select(activity => new { AcademicYear = activity["Academic year"], Term = activity["Term"] })
                .Distinct()
                .OrderBy(period => period.AcademicYear)
                .ThenBy(period => period.Term)
                .ToList();
            var graduationYear = periods.Count == 0 ? "Graduation year unavailable" : periods[^1].AcademicYear;
            return record with
            {
                Id = PeriodId(record.ResourceId, "GRADUATED", "FULL-PROGRAM"),
                Activities = activities,
                AcademicYear = graduationYear,
                Term = "Completed Year 4 Semester 2",
                Status = "Graduated",
                Summary = $"Permanent four-year archive · {periods.Count} semesters · {activities.Count} recorded activities",
                Insights = null
            };
        }).ToList();

    private static OperationalRecordDto AddStudentInsights(OperationalRecordDto record, GradeThresholds thresholds, bool applyAttendanceRules, string academicYear, string term, bool fullProgram)
    {
        var attendance = record.Activities.Where(activity => activity.GetValueOrDefault("Activity") == "Class attendance").Select(activity => activity.GetValueOrDefault("Attendance", "")).ToList();
        var grades = record.Activities
            .Where(activity => activity.GetValueOrDefault("Activity") == "Course grade")
            .GroupBy(activity => fullProgram
                ? $"{activity.GetValueOrDefault("Academic year")}|{activity.GetValueOrDefault("Term")}|{activity.GetValueOrDefault("Course code", "—")}"
                : activity.GetValueOrDefault("Course code", "—"))
            .Select(group => group.First())
            .Take(fullProgram ? int.MaxValue : SemesterResultRules.ExpectedCourseCount)
            .Select(activity => new OperationalRecordGradeDto(
                Guid.TryParse(activity.GetValueOrDefault("CourseId"), out var courseId) ? courseId : Guid.Empty,
                activity.GetValueOrDefault("Grade code", "GRD-NOT-RECORDED"),
                activity.GetValueOrDefault("Course code", "—"),
                activity.GetValueOrDefault("Course", "Course"),
                ParseScore(activity.GetValueOrDefault("Score")),
                activity.GetValueOrDefault("Grade", "F")))
            .ToList();
        var present = attendance.Count(status => status is "Present" or "Late");
        var permission = attendance.Count(status => status is "Permission" or "Excused");
        var absent = attendance.Count(status => status == "Absent");
        var total = grades.Sum(grade => grade.Score);
        var average = fullProgram
            ? grades.Count == 0 ? 0 : decimal.Round(total / grades.Count, 2)
            : SemesterResultRules.Average(grades.Select(grade => grade.Score));
        var isFinal = fullProgram || record.AcademicYear != academicYear || record.Term != term;
        var result = fullProgram
            ? "Graduated"
            : isFinal
            ? SemesterResultRules.Outcome(absent, grades.Select(grade => grade.Grade).ToList(), average, thresholds, applyAttendanceRules)
            : "In progress";
        var expectedCourses = fullProgram ? grades.Count : SemesterResultRules.ExpectedCourseCount;
        var insights = new OperationalRecordInsightsDto(present, permission, absent, grades, expectedCourses, total, average, result, isFinal);
        return record with { Summary = fullProgram
            ? $"Four-year total · {present:N0} present · {permission:N0} permission · {absent:N0} absent · {grades.Count:N0} course grades"
            : $"{attendance.Count:N0} class sessions · {grades.Count}/{SemesterResultRules.ExpectedCourseCount} course grades", Insights = insights };
    }

    private static decimal ParseScore(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var score) ? score : 0;

    private static Guid PeriodId(Guid resourceId, string academicYear, string term)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{resourceId:N}|{academicYear}|{term}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    private enum PeriodScope { Current, Closed, All }
}
