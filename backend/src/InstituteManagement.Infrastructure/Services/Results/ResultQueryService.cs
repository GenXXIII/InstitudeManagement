using System.Text.Json;
using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Results;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Results;

public sealed class ResultQueryService(InstituteDbContext db) : IResultQueryService
{
    public async Task<IReadOnlyList<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, CancellationToken cancellationToken)
    {
        var students = await db.Students.AsNoTracking().Include(student => student.Department)
            .Where(student => (!departmentId.HasValue || student.DepartmentId == departmentId) && (!year.HasValue || student.YearLevel == year))
            .ToListAsync(cancellationToken);
        var studentIds = students.Select(student => student.Id).ToHashSet();
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(record => studentIds.Contains(record.StudentId)).ToListAsync(cancellationToken);
        var grades = await db.GradeRecords.AsNoTracking().Include(record => record.Course).Where(record => studentIds.Contains(record.StudentId)).ToListAsync(cancellationToken);
        var sessionRows = await db.ClassSessionRecords.AsNoTracking()
            .Where(record => (!departmentId.HasValue || record.DepartmentId == departmentId) && (!year.HasValue || record.YearLevel == year))
            .Select(record => new { record.AcademicYear, record.Term, record.StudentAttendanceJson })
            .ToListAsync(cancellationToken);
        var sessionAttendance = sessionRows.SelectMany(record => Deserialize(record.StudentAttendanceJson).Select(student => new SessionAttendance(student.StudentId, record.AcademicYear, record.Term, student.Status))).Where(record => studentIds.Contains(record.StudentId)).ToList();
        var settings = await db.SystemSettings.AsNoTracking().Where(setting => setting.Section == "academic-year" || setting.Section == "semester" || setting.Section == "grade-rules").ToListAsync(cancellationToken);
        var thresholds = GradeThresholds.From(settings.Where(setting => setting.Section == "grade-rules").ToDictionary(setting => setting.Key, setting => setting.Value));
        var results = new List<SemesterResultDto>();

        foreach (var student in students)
        {
            var periods = attendance.Where(record => record.StudentId == student.Id).Select(record => new Period(record.AcademicYear, record.Term))
                .Concat(grades.Where(record => record.StudentId == student.Id).Select(record => new Period(record.AcademicYear, record.Term)))
                .Concat(sessionAttendance.Where(record => record.StudentId == student.Id).Select(record => new Period(record.AcademicYear, record.Semester)))
                .Distinct()
                .Where(period => (string.IsNullOrWhiteSpace(semester) || period.Semester.Equals(semester, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(academicYear) || period.AcademicYear.Equals(academicYear, StringComparison.OrdinalIgnoreCase)));

            foreach (var period in periods)
            {
                var periodAttendance = attendance.Where(record => record.StudentId == student.Id && record.AcademicYear == period.AcademicYear && record.Term == period.Semester).ToList();
                var timetableAttendance = sessionAttendance.Where(record => record.StudentId == student.Id && record.AcademicYear == period.AcademicYear && record.Semester == period.Semester).ToList();
                var periodGrades = grades.Where(record => record.StudentId == student.Id && record.AcademicYear == period.AcademicYear && record.Term == period.Semester)
                    .OrderBy(record => record.Course!.Code)
                    .Select(record => new CourseResultDto(record.CourseId, record.Course?.Code ?? "—", record.Course?.Name ?? "Course", record.Score, record.LetterGrade))
                    .ToList();
                var total = periodGrades.Sum(grade => grade.Score);
                var average = periodGrades.Count == 0 ? 0 : decimal.Round(total / periodGrades.Count, 2);
                var statuses = timetableAttendance.Count > 0 ? timetableAttendance.Select(record => record.Status).ToList() : periodAttendance.Select(record => record.Status).ToList();
                var absent = statuses.Count(status => status == "Absent");
                var totalGrade = absent >= 8 ? "Fail" : absent >= 6 ? "Retake Exam" : periodGrades.Count == 0 ? "Pending" : thresholds.Letter(average);
                results.Add(new SemesterResultDto(
                    student.Id, student.StudentNumber, student.FullName, student.DepartmentId, student.Department?.Name ?? "Unassigned", student.YearLevel,
                    period.AcademicYear, period.Semester,
                    statuses.Count(status => status is "Present" or "Late"), absent,
                    statuses.Count(status => status is "Excused" or "Permission"),
                    periodGrades, periodGrades.Count, total, average, totalGrade));
            }
        }

        return results.OrderBy(result => result.Year).ThenBy(result => result.FullName).ThenByDescending(result => result.AcademicYear).ThenBy(result => result.Semester).ToList();
    }

    private sealed record Period(string AcademicYear, string Semester);
    private sealed record SessionAttendance(Guid StudentId, string AcademicYear, string Semester, string Status);
    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
