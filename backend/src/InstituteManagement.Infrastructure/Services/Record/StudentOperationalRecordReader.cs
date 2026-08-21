using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class StudentOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "students";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var students = await db.Students.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = students.Select(x => x.Id).ToList();
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var grades = await db.GradeRecords.AsNoTracking().Include(x => x.Course).Where(x => ids.Contains(x.StudentId)).ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).ToListAsync(cancellationToken);
        var studentSessions = sessions.SelectMany(session => Deserialize(session.StudentAttendanceJson).Select(student => (Session: session, Student: student))).Where(x => ids.Contains(x.Student.StudentId)).ToList();
        return students.Select(student =>
        {
            var completed = studentSessions.Where(x => x.Student.StudentId == student.Id).ToList();
            var events = attendance.Where(x => x.StudentId == student.Id).Select(x => (At: AttendanceDate(x), Values: Create(("Activity", "Attendance"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.Date.ToString("yyyy-MM-dd")), ("Time", x.CheckedInAt?.ToString("HH:mm") ?? "—"), ("Status", x.Status), ("Method", x.Method))))
                .Concat(grades.Where(x => x.StudentId == student.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Assessment"), ("Course", x.Course?.Name ?? "—"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Score", x.Score.ToString("0.0")), ("Grade", x.LetterGrade)))))
                .Concat(completed.Select(x => (x.Session.UpdatedAtUtc, Create(("Activity", "Completed class"), ("Academic year", x.Session.AcademicYear), ("Term", x.Session.Term), ("Date", x.Session.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.Session.StartsAt:HH:mm} – {x.Session.EndsAt:HH:mm}"), ("Course", x.Session.CourseName), ("Teacher", x.Session.TeacherName), ("Classroom", x.Session.ClassroomCode), ("Attendance", x.Student.Status), ("Check in", string.IsNullOrWhiteSpace(x.Student.CheckedInAt) ? "No check-in" : x.Student.CheckedInAt)))))
                .OrderByDescending(x => x.Item1).ToList();
            return new OperationalRecordDto(student.Id, "Student", student.FullName, student.StudentNumber, student.Status, $"{completed.Count} completed classes · {events.Count(x => x.Item2["Activity"] == "Attendance")} attendance · {events.Count(x => x.Item2["Activity"] == "Assessment")} assessments", events.Count == 0 ? null : events[0].Item1, events.Select(x => x.Item2).ToList());
        }).ToList();
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
