using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassroomOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "classrooms";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var rooms = await db.Classrooms.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var ids = rooms.Select(x => x.Id).ToList();
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.ClassroomId)).ToListAsync(cancellationToken);
        return rooms.Select(room =>
        {
            var completed = sessions.Where(x => x.ClassroomId == room.Id).OrderByDescending(x => x.UpdatedAtUtc).ToList();
            var events = completed.Select(x => Create(("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Teacher", x.TeacherName), ("Attendance", $"{x.PresentCount} present · {x.LateCount} late · {x.AbsentCount} absent · {x.ExcusedCount} excused"))).ToList();
            return new OperationalRecordDto(room.Id, "Classroom", room.Code, room.Building, room.Status, $"{events.Count} completed timetable classes", completed.Count == 0 ? null : completed[0].UpdatedAtUtc, events);
        }).ToList();
    }
}
