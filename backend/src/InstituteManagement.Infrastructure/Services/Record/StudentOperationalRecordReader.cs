using System.Globalization;
using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class StudentOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "students";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var students = await db.Students.AsNoTracking().Include(x => x.Department).Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = students.Select(x => x.Id).ToList();
        var enrollments = await db.StudentEnrollments.AsNoTracking().Include(x => x.Department).Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Include(x => x.ScheduleEntry).Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).ToListAsync(cancellationToken);
        var grades = await db.GradeRecords.AsNoTracking().Include(x => x.Course).Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var studentSessions = sessions.SelectMany(session => Deserialize(session.StudentAttendanceJson).Select(student => (Session: session, Student: student))).Where(x => ids.Contains(x.Student.StudentId)).ToList();
        return students.Select(student =>
        {
            var completed = studentSessions.Where(x => x.Student.StudentId == student.Id).ToList();
            var studentGrades = grades.Where(x => x.StudentId == student.Id).ToList();
            var enrollmentEvents = enrollments.Where(x => x.StudentId == student.Id).Select(x => (At: x.UpdatedAtUtc, Activity: Create(("Activity", "Student enrollment"), ("Academic year", x.AcademicYear), ("Term", x.Semester), ("Date", x.UpdatedAtUtc.ToString("yyyy-MM-dd")), ("Time", x.UpdatedAtUtc.ToString("HH:mm")), ("Year", $"Year {x.YearLevel}"), ("Shift", x.Shift), ("Department", x.Department?.Name ?? student.Department?.Name ?? "Unassigned"), ("Enrollment status", x.Status))));
            var attendanceEvents = completed.Select(x => (At: x.Session.UpdatedAtUtc, Activity: Create(
                ("Activity", "Class attendance"), ("ClassSessionId", x.Session.Id.ToString()),
                ("Class session code", SessionCode(x.Session)), ("Timetable code", x.Session.ScheduleEntry?.TimetableCode ?? "Not recorded"),
                ("Academic year", x.Session.AcademicYear), ("Term", x.Session.Term),
                ("Date", x.Session.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.Session.StartsAt:HH:mm} – {x.Session.EndsAt:HH:mm}"),
                ("Year", $"Year {x.Session.YearLevel}"), ("Course", x.Session.CourseName),
                ("Teacher", x.Session.TeacherName), ("Classroom", x.Session.ClassroomCode),
                ("Teacher attendance", x.Session.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.Session.TeacherAttendanceStatus)),
                ("Reason", TeacherPresence.Reason(x.Session.TeacherAttendanceStatus)), ("Attendance", x.Student.Status),
                ("Check in", string.IsNullOrWhiteSpace(x.Student.CheckedInAt) ? "No check-in" : x.Student.CheckedInAt))));
            var gradeEvents = studentGrades.Select(x => (At: x.UpdatedAtUtc, Activity: Create(
                ("Activity", "Course grade"), ("CourseId", x.CourseId.ToString()), ("Grade code", x.GradeCode),
                ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.UpdatedAtUtc.ToString("yyyy-MM-dd")),
                ("Time", x.UpdatedAtUtc.ToString("HH:mm")), ("Course code", x.Course?.CourseCode ?? "—"),
                ("Course", x.Course?.Name ?? "Course"), ("Score", x.Score.ToString("0.##", CultureInfo.InvariantCulture)),
                ("Grade", x.LetterGrade))));
            var events = enrollmentEvents.Concat(attendanceEvents).Concat(gradeEvents).OrderByDescending(x => x.At).ToList();
            return new OperationalRecordDto(
                student.Id,
                "Student",
                student.FullName,
                $"{student.StudentCode} · Year {student.YearLevel} · {student.Shift}",
                student.Status,
                $"{completed.Count} recorded class sessions · {studentGrades.Count} recorded course grades",
                events.Count == 0 ? null : events[0].At,
                events.Select(x => x.Activity).ToList(),
                Code: student.StudentCode,
                PhotoDataUrl: student.PhotoDataUrl,
                Department: student.Department?.Name ?? "Unassigned",
                ResourceId: student.Id);
        }).ToList();
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string SessionCode(ClassSessionRecord session)
    {
        if (session.ClassSessionRecordCode.StartsWith("SES-", StringComparison.OrdinalIgnoreCase) && session.ClassSessionRecordCode.Length <= 32)
            return session.ClassSessionRecordCode.ToUpperInvariant();
        var timetable = session.ScheduleEntry?.TimetableCode ?? session.ScheduleEntryId.ToString("N")[..8];
        var suffix = timetable.Contains('-') ? timetable[(timetable.IndexOf('-') + 1)..] : timetable;
        return $"SES-{session.SessionDate:yyyyMMdd}-{suffix}".ToUpperInvariant();
    }
}
