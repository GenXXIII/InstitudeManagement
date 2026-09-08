using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class ActivePeriodLedgerCreator(InstituteDbContext db)
{
    public async Task<(int Attendance, int Grades)> CreateAsync(
        string academicYear,
        string term,
        DateOnly startsOn,
        CancellationToken cancellationToken)
    {
        var students = await GetActiveStudentsAsync(cancellationToken);
        if (students.Count == 0) return (0, 0);

        var existingAttendance = (await db.AttendanceRecords.AsNoTracking()
            .Where(record => record.AcademicYear == academicYear && record.Term == term)
            .Select(record => record.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var existingGrades = (await db.GradeRecords.AsNoTracking()
            .Where(record => record.AcademicYear == academicYear && record.Term == term)
            .Select(record => record.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var schedules = await db.ScheduleEntries.AsNoTracking()
            .Include(entry => entry.Course)
            .Where(entry => entry.Status != "Cancelled")
            .OrderBy(entry => entry.TimetableCode)
            .ToListAsync(cancellationToken);
        var attendanceMethod = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "attendance-rules" && setting.Key == "method")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "ID Card";

        var attendanceCodes = new Queue<string>(await BusinessCodeFormatter.GenerateManyAsync(db, "attendance", students.Count, cancellationToken));
        var gradeCodes = new Queue<string>(await BusinessCodeFormatter.GenerateManyAsync(db, "grade", students.Count, cancellationToken));
        var attendanceCreated = 0;
        var gradesCreated = 0;
        foreach (var student in students)
        {
            if (!existingAttendance.Contains(student.Id))
            {
                db.AttendanceRecords.Add(CreateAttendance(student, attendanceCodes.Dequeue(), academicYear, term, startsOn, attendanceMethod));
                attendanceCreated++;
            }

            if (existingGrades.Contains(student.Id)) continue;
            var courseId = FindCourseId(student, schedules);
            if (!courseId.HasValue) continue;
            db.GradeRecords.Add(new GradeRecord
            {
                GradeCode = gradeCodes.Dequeue(),
                StudentId = student.Id,
                CourseId = courseId.Value,
                Score = 0,
                LetterGrade = "F",
                AcademicYear = academicYear,
                Term = term
            });
            gradesCreated++;
        }
        return (attendanceCreated, gradesCreated);
    }

    private async Task<List<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken)
    {
        var tracked = db.ChangeTracker.Entries<Student>()
            .Where(entry => entry.State is not EntityState.Deleted and not EntityState.Detached)
            .ToDictionary(entry => entry.Entity.Id, entry => entry.Entity);
        return (await db.Students.AsNoTracking().ToListAsync(cancellationToken))
            .Select(student => tracked.GetValueOrDefault(student.Id, student))
            .Where(student => student.Status != "Inactive")
            .ToList();
    }

    private static AttendanceRecord CreateAttendance(
        Student student,
        string attendanceCode,
        string academicYear,
        string term,
        DateOnly startsOn,
        string method) => new()
        {
            AttendanceCode = attendanceCode,
            StudentId = student.Id,
            Date = startsOn,
            CheckedInAt = RequiredShift(student.Shift).StartsAt,
            Status = "Present",
            Method = method,
            AcademicYear = academicYear,
            Term = term
        };

    private static Guid? FindCourseId(Student student, IEnumerable<ScheduleEntry> schedules) =>
        schedules.FirstOrDefault(entry =>
            entry.YearLevel == student.YearLevel
            && entry.Course?.DepartmentId == student.DepartmentId
            && AcademicTimetablePolicy.FindShift(entry.DayOfWeek, entry.StartsAt, entry.EndsAt)?.Name == student.Shift)?.CourseId
        ?? schedules.FirstOrDefault(entry =>
            entry.YearLevel == student.YearLevel
            && entry.Course?.DepartmentId == student.DepartmentId)?.CourseId;

    private static AcademicShift RequiredShift(string name) =>
        AcademicTimetablePolicy.FindShift(name)
        ?? throw new InvalidOperationException("Student shift is not configured in the academic timetable policy.");
}
