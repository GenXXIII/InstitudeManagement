using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class TimetableOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "timetable";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var schedules = await db.ScheduleEntries.AsNoTracking()
            .Include(x => x.Course)!.ThenInclude(x => x!.Department)
            .Include(x => x.Teacher).Include(x => x.Classroom)
            .Where(x => !departmentId.HasValue || x.Course!.DepartmentId == departmentId)
            .OrderBy(x => x.TimetableCode).ToListAsync(cancellationToken);
        var ids = schedules.Select(x => x.Id).ToList();
        var courseIds = schedules.Select(x => x.CourseId).Distinct().ToList();
        var enrollments = await db.TimetableEnrollments.AsNoTracking().Where(x => ids.Contains(x.ScheduleEntryId)).ToListAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking().Include(x => x.Department)
            .Where(x => courseIds.Contains(x.CourseId) && (!departmentId.HasValue || x.DepartmentId == departmentId)).ToListAsync(cancellationToken);
        var departmentIds = courseAssignments.Select(x => x.DepartmentId).Distinct().ToList();
        var students = await db.StudentEnrollments.AsNoTracking().Where(x => departmentIds.Contains(x.DepartmentId) && x.Status == "Active").ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.ScheduleEntryId)).ToListAsync(cancellationToken);

        return schedules.Select(schedule =>
        {
            var scheduleEnrollments = enrollments.Where(x => x.ScheduleEntryId == schedule.Id).ToList();
            var completed = sessions.Where(x => x.ScheduleEntryId == schedule.Id).ToList();
            var shift = AcademicTimetablePolicy.FindShift(schedule.DayOfWeek, schedule.StartsAt, schedule.EndsAt);
            var enrollmentEvents = scheduleEnrollments.Select(enrollment =>
            {
                var assignment = courseAssignments.FirstOrDefault(x => x.CourseId == schedule.CourseId && x.AcademicYear == enrollment.AcademicYear && x.Semester == enrollment.Semester);
                var enrolledStudents = assignment is null ? 0 : students.Count(x => x.DepartmentId == assignment.DepartmentId && x.YearLevel == schedule.YearLevel && x.AcademicYear == enrollment.AcademicYear && x.Semester == enrollment.Semester && (shift == null || x.Shift == shift.Name));
                return (enrollment.UpdatedAtUtc, Create(
                    ("Activity", "Timetable enrollment"), ("Academic year", enrollment.AcademicYear), ("Term", enrollment.Semester),
                    ("Date", enrollment.UpdatedAtUtc.ToString("yyyy-MM-dd")), ("Time", $"{schedule.StartsAt:HH:mm} – {schedule.EndsAt:HH:mm}"),
                    ("Day", schedule.DayOfWeek.ToString()), ("Year", $"Year {schedule.YearLevel}"),
                    ("Course", schedule.Course?.Name ?? "Course"), ("Course code", schedule.Course?.CourseCode ?? "—"),
                    ("Teacher", schedule.Teacher?.FullName ?? "Not assigned"), ("Teacher code", schedule.Teacher?.TeacherCode ?? "—"),
                    ("Classroom", schedule.Classroom?.ClassroomCode ?? "—"),
                    ("Department", assignment?.Department?.Name ?? schedule.Course?.Department?.Name ?? "Unassigned"),
                    ("Shift", shift?.Name ?? "Unmatched"), ("Student count", enrolledStudents.ToString()),
                    ("Enrollment status", enrollment.Status)));
            });
            var sessionEvents = completed.Select(x => (x.UpdatedAtUtc, Create(
                ("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term),
                ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"),
                ("Day", x.SessionDate.DayOfWeek.ToString()), ("Year", $"Year {x.YearLevel}"),
                ("Course", x.CourseName), ("Teacher", x.TeacherName), ("Classroom", x.ClassroomCode),
                ("Teacher attendance", x.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.TeacherAttendanceStatus)),
                ("Reason", TeacherPresence.Reason(x.TeacherAttendanceStatus)),
                ("Student count", x.StudentCount.ToString()),
                ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"))));
            var events = enrollmentEvents.Concat(sessionEvents).OrderByDescending(x => x.Item1).ToList();
            var status = schedule.Status == "Cancelled" ? "Cancelled" : scheduleEnrollments.Any(x => x.Status == "Active") ? "Enrolled" : schedule.Status;
            return new OperationalRecordDto(schedule.Id, "Timetable", schedule.Course?.Name ?? "Scheduled course",
                $"{schedule.DayOfWeek} · {schedule.StartsAt:HH:mm}–{schedule.EndsAt:HH:mm}", status,
                $"{completed.Count} recorded timetable periods", events.Count == 0 ? null : events[0].Item1,
                events.Select(x => x.Item2).ToList(), Code: schedule.TimetableCode,
                Department: schedule.Course?.Department?.Name ?? "Unassigned", ResourceId: schedule.Id);
        }).ToList();
    }
}
