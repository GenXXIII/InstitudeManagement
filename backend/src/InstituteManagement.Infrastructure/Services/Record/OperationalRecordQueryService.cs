using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
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

        records = SplitByPeriod(records, academicYear, term, history)
            .Select(record => record.Module == "Student" ? AddStudentInsights(record, thresholds, applyAttendanceRules, academicYear, term) : record)
            .ToList();
        if (string.IsNullOrWhiteSpace(search)) return records;
        var searchTerm = search.Trim();
        return records.Where(x => Matches(searchTerm, x.Subject, x.Code, x.Department, x.Identifier, x.Summary, x.AcademicYear, x.Term)
            || x.Activities.Any(activity => activity.Values.Any(value => value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))).ToList();
    }

    private static IReadOnlyList<OperationalRecordDto> SplitByPeriod(IReadOnlyList<OperationalRecordDto> records, string academicYear, string term, bool history)
    {
        var periodRecords = new List<OperationalRecordDto>();
        foreach (var record in records)
        {
            var groups = record.Activities
                .Where(activity => !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Academic year")) && !string.IsNullOrWhiteSpace(activity.GetValueOrDefault("Term")))
                .GroupBy(activity => new { AcademicYear = activity["Academic year"], Term = activity["Term"] })
                .Where(group => history || group.Key.AcademicYear == academicYear && group.Key.Term == term);
            foreach (var group in groups)
            {
                var activities = group.ToList();
                periodRecords.Add(record with
                {
                    Id = record.Module == "Session" ? record.Id : PeriodId(record.Id, group.Key.AcademicYear, group.Key.Term),
                    Activities = activities,
                    Summary = $"{activities.Count:N0} recorded activities",
                    Status = record.Module == "Teacher" ? activities.Select(activity => activity.GetValueOrDefault("Teacher attendance")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? record.Status : record.Status,
                    AcademicYear = group.Key.AcademicYear,
                    Term = group.Key.Term,
                    Insights = null
                });
            }
        }
        return periodRecords;
    }

    private static OperationalRecordDto AddStudentInsights(OperationalRecordDto record, GradeThresholds thresholds, bool applyAttendanceRules, string academicYear, string term)
    {
        var attendance = record.Activities.Where(activity => activity.GetValueOrDefault("Activity") == "Class attendance").Select(activity => activity.GetValueOrDefault("Attendance", "")).ToList();
        var grades = record.Activities
            .Where(activity => activity.GetValueOrDefault("Activity") == "Course grade")
            .GroupBy(activity => activity.GetValueOrDefault("Course code", "—"))
            .Select(group => group.First())
            .Take(SemesterResultRules.ExpectedCourseCount)
            .Select(activity => new OperationalRecordGradeDto(
                Guid.TryParse(activity.GetValueOrDefault("Course id"), out var courseId) ? courseId : Guid.Empty,
                activity.GetValueOrDefault("Course code", "—"),
                activity.GetValueOrDefault("Course", "Course"),
                ParseScore(activity.GetValueOrDefault("Score")),
                activity.GetValueOrDefault("Grade", "F")))
            .ToList();
        var present = attendance.Count(status => status is "Present" or "Late");
        var permission = attendance.Count(status => status is "Permission" or "Excused");
        var absent = attendance.Count(status => status == "Absent");
        var total = grades.Sum(grade => grade.Score);
        var average = SemesterResultRules.Average(grades.Select(grade => grade.Score));
        var isFinal = record.AcademicYear != academicYear || record.Term != term;
        var result = isFinal
            ? SemesterResultRules.Outcome(absent, grades.Select(grade => grade.Grade).ToList(), average, thresholds, applyAttendanceRules)
            : "In progress";
        var insights = new OperationalRecordInsightsDto(present, permission, absent, grades, SemesterResultRules.ExpectedCourseCount, total, average, result, isFinal);
        return record with { Summary = $"{attendance.Count:N0} class sessions · {grades.Count}/{SemesterResultRules.ExpectedCourseCount} course grades", Insights = insights };
    }

    private static decimal ParseScore(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var score) ? score : 0;

    private static Guid PeriodId(Guid resourceId, string academicYear, string term)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{resourceId:N}|{academicYear}|{term}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
}
