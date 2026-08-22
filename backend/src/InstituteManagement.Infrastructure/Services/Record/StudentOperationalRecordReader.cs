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
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).ToListAsync(cancellationToken);
        var studentSessions = sessions.SelectMany(session => Deserialize(session.StudentAttendanceJson).Select(student => (Session: session, Student: student))).Where(x => ids.Contains(x.Student.StudentId)).ToList();
        return students.Select(student =>
        {
            var completed = studentSessions.Where(x => x.Student.StudentId == student.Id).ToList();
            var events = completed.Select(x => (x.Session.UpdatedAtUtc, Create(("Activity", "Class attendance"), ("Academic year", x.Session.AcademicYear), ("Term", x.Session.Term), ("Date", x.Session.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.Session.StartsAt:HH:mm} – {x.Session.EndsAt:HH:mm}"), ("Year", $"Year {x.Session.YearLevel}"), ("Course", x.Session.CourseName), ("Teacher", x.Session.TeacherName), ("Classroom", x.Session.ClassroomCode), ("Attendance", x.Student.Status), ("Check in", string.IsNullOrWhiteSpace(x.Student.CheckedInAt) ? "No check-in" : x.Student.CheckedInAt))))
                .OrderByDescending(x => x.Item1).ToList();
            return new OperationalRecordDto(student.Id, "Student", student.FullName, $"{student.StudentCode} · Year {student.YearLevel}", student.Status, $"{completed.Count} completed-class attendance records", events.Count == 0 ? null : events[0].Item1, events.Select(x => x.Item2).ToList());
        }).ToList();
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
