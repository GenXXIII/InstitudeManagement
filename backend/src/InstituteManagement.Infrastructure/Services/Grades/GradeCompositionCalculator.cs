using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

internal sealed record GradeAttendanceEvidence(decimal Score, int Attended, int Sessions);

internal static class GradeCompositionCalculator
{
    public static async Task<(GradeWeights Weights, GradeThresholds Thresholds)> LoadRulesAsync(
        InstituteDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "grade-rules")
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
        return (GradeWeights.From(settings), GradeThresholds.From(settings));
    }

    public static async Task<GradeAttendanceEvidence> AttendanceAsync(
        InstituteDbContext db,
        Guid studentId,
        Guid courseId,
        string academicYear,
        string term,
        decimal maximum,
        CancellationToken cancellationToken)
    {
        var sessions = await db.ClassSessionRecords.AsNoTracking()
            .Where(session => session.CourseId == courseId && session.AcademicYear == academicYear && session.Term == term)
            .ToListAsync(cancellationToken);
        return Attendance(sessions, studentId, maximum);
    }

    public static async Task RefreshGradesAsync(
        InstituteDbContext db,
        Guid courseId,
        string academicYear,
        string term,
        CancellationToken cancellationToken)
    {
        var grades = await db.GradeRecords
            .Where(grade => grade.CourseId == courseId && grade.AcademicYear == academicYear && grade.Term == term)
            .ToListAsync(cancellationToken);
        if (grades.Count == 0) return;
        var rules = await LoadRulesAsync(db, cancellationToken);
        var sessions = await db.ClassSessionRecords
            .Where(session => session.CourseId == courseId && session.AcademicYear == academicYear && session.Term == term)
            .ToListAsync(cancellationToken);
        sessions.AddRange(db.ChangeTracker.Entries<ClassSessionRecord>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.CourseId == courseId && entry.Entity.AcademicYear == academicYear && entry.Entity.Term == term)
            .Select(entry => entry.Entity)
            .Where(added => sessions.All(existing => existing.Id != added.Id)));
        foreach (var grade in grades)
        {
            var attendance = Attendance(sessions, grade.StudentId, rules.Weights.Attendance);
            Apply(grade, rules.Weights, rules.Thresholds, attendance, grade.AssignmentScore, grade.MidtermScore, grade.FinalExamScore);
            grade.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public static GradeAttendanceEvidence Attendance(IEnumerable<ClassSessionRecord> sessions, Guid studentId, decimal maximum)
    {
        var held = sessions.Where(session => TeacherPresence.SessionStatus(session.TeacherAttendanceStatus) == "Running").ToList();
        var attended = held.Count(session => Students(session.StudentAttendanceJson)
            .Any(student => student.StudentId == studentId && student.Status is "Present" or "Late"));
        var score = held.Count == 0
            ? 0
            : decimal.Round(maximum * attended / held.Count, 2, MidpointRounding.AwayFromZero);
        return new GradeAttendanceEvidence(score, attended, held.Count);
    }

    public static void Apply(
        GradeRecord grade,
        GradeWeights weights,
        GradeThresholds thresholds,
        GradeAttendanceEvidence attendance,
        decimal assignment,
        decimal midterm,
        decimal finalExam)
    {
        Validate("Assignment", assignment, weights.Assignment);
        Validate("Midterm", midterm, weights.Midterm);
        Validate("Final exam", finalExam, weights.FinalExam);

        grade.AttendanceScore = attendance.Score;
        grade.AttendanceMaximum = weights.Attendance;
        grade.AssignmentScore = assignment;
        grade.AssignmentMaximum = weights.Assignment;
        grade.MidtermScore = midterm;
        grade.MidtermMaximum = weights.Midterm;
        grade.FinalExamScore = finalExam;
        grade.FinalExamMaximum = weights.FinalExam;
        grade.Score = weights.Score(attendance.Score, assignment, midterm, finalExam);
        grade.LetterGrade = thresholds.Letter(grade.Score);
    }

    private static void Validate(string label, decimal value, decimal maximum)
    {
        if (value < 0 || value > maximum)
            throw new ArgumentOutOfRangeException(label.Replace(" ", string.Empty), $"{label} score must be between 0 and {maximum:0.##}.");
    }

    private static IReadOnlyList<SessionStudentSnapshot> Students(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
