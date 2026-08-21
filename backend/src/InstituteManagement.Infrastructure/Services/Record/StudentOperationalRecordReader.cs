using InstituteManagement.Application.DTOs;
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
        return students.Select(student =>
        {
            var events = attendance.Where(x => x.StudentId == student.Id).Select(x => (At: AttendanceDate(x), Values: Create(("Activity", "Attendance"), ("Date", x.Date.ToString("yyyy-MM-dd")), ("Time", x.CheckedInAt?.ToString("HH:mm") ?? "—"), ("Status", x.Status), ("Method", x.Method))))
                .Concat(grades.Where(x => x.StudentId == student.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Assessment"), ("Course", x.Course?.Name ?? "—"), ("Term", x.Term), ("Score", x.Score.ToString("0.0")), ("Grade", x.LetterGrade)))))
                .OrderByDescending(x => x.Item1).ToList();
            return new OperationalRecordDto(student.Id, "Student", student.FullName, student.StudentNumber, student.Status, $"{events.Count(x => x.Item2["Activity"] == "Attendance")} attendance · {events.Count(x => x.Item2["Activity"] == "Assessment")} assessments", events.Count == 0 ? null : events[0].Item1, events.Select(x => x.Item2).ToList());
        }).ToList();
    }
}
