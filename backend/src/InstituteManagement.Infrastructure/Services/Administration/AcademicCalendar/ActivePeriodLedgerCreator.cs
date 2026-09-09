using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Enrollment;
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
        var studentEnrollments = await GetActiveStudentEnrollmentsAsync(academicYear, term, cancellationToken);
        if (studentEnrollments.Count == 0) return (0, 0);

        var existingAttendance = (await db.AttendanceRecords.AsNoTracking()
            .Where(record => record.AcademicYear == academicYear && record.Term == term)
            .Select(record => record.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var existingGrades = (await db.GradeRecords.AsNoTracking()
            .Where(record => record.AcademicYear == academicYear && record.Term == term)
            .Select(record => record.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var timetableEnrollments = await db.TimetableEnrollments.AsNoTracking()
            .Include(enrollment => enrollment.ScheduleEntry)
            .Include(enrollment => enrollment.Course)
            .Where(enrollment =>
                enrollment.AcademicYear == academicYear
                && enrollment.Semester == term
                && enrollment.Status == "Active"
                && enrollment.ScheduleEntry != null
                && enrollment.ScheduleEntry.Status != "Cancelled")
            .OrderBy(enrollment => enrollment.ScheduleEntry!.TimetableCode)
            .ToListAsync(cancellationToken);
        var attendanceMethod = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "attendance-rules" && setting.Key == "method")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "ID Card";

        var attendanceCodes = new Queue<string>(await BusinessCodeFormatter.GenerateManyAsync(db, "attendance", studentEnrollments.Count, cancellationToken));
        var gradeCodes = new Queue<string>(await BusinessCodeFormatter.GenerateManyAsync(db, "grade", studentEnrollments.Count, cancellationToken));
        var attendanceCreated = 0;
        var gradesCreated = 0;
        foreach (var enrollment in studentEnrollments)
        {
            var student = enrollment.Student;
            if (!existingAttendance.Contains(student.Id))
            {
                db.AttendanceRecords.Add(CreateAttendance(
                    student,
                    enrollment.Shift,
                    attendanceCodes.Dequeue(),
                    academicYear,
                    term,
                    startsOn,
                    attendanceMethod));
                attendanceCreated++;
            }

            if (existingGrades.Contains(student.Id)) continue;
            var courseId = FindCourseId(enrollment, timetableEnrollments);
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

    private async Task<List<ActiveStudentEnrollment>> GetActiveStudentEnrollmentsAsync(
        string academicYear,
        string term,
        CancellationToken cancellationToken)
    {
        var enrollments = (await db.StudentEnrollments.AsNoTracking()
                .Where(enrollment =>
                    enrollment.AcademicYear == academicYear
                    && enrollment.Semester == term)
                .ToListAsync(cancellationToken))
            .ToDictionary(enrollment => enrollment.StudentId);
        foreach (var tracked in db.ChangeTracker.Entries<StudentEnrollment>()
                     .Where(entry =>
                         entry.State is not EntityState.Deleted and not EntityState.Detached
                         && entry.Entity.AcademicYear == academicYear
                         && entry.Entity.Semester == term))
        {
            enrollments[tracked.Entity.StudentId] = tracked.Entity;
        }

        var studentIds = enrollments.Keys.ToList();
        var students = (await db.Students.AsNoTracking()
                .Where(student => studentIds.Contains(student.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(student => student.Id);
        foreach (var tracked in db.ChangeTracker.Entries<Student>()
                     .Where(entry => studentIds.Contains(entry.Entity.Id) && entry.State is not EntityState.Deleted and not EntityState.Detached))
        {
            students[tracked.Entity.Id] = tracked.Entity;
        }

        return enrollments.Values
            .Where(enrollment =>
                enrollment.Status == "Active"
                && students.TryGetValue(enrollment.StudentId, out var student)
                && student.Status != "Inactive")
            .Select(enrollment => new ActiveStudentEnrollment(
                students[enrollment.StudentId],
                enrollment.DepartmentId,
                enrollment.YearLevel,
                enrollment.Shift,
                enrollment.AcademicYear,
                enrollment.Semester))
            .ToList();
    }

    private static AttendanceRecord CreateAttendance(
        Student student,
        string shift,
        string attendanceCode,
        string academicYear,
        string term,
        DateOnly startsOn,
        string method) => new()
        {
            AttendanceCode = attendanceCode,
            StudentId = student.Id,
            Date = startsOn,
            CheckedInAt = RequiredShift(shift).StartsAt,
            Status = "Present",
            Method = method,
            AcademicYear = academicYear,
            Term = term
        };

    private static Guid? FindCourseId(
        ActiveStudentEnrollment studentEnrollment,
        IEnumerable<TimetableEnrollment> timetableEnrollments)
    {
        var studentCohort = EnrollmentCohortKey.Create(
            studentEnrollment.DepartmentId,
            studentEnrollment.YearLevel,
            studentEnrollment.Shift,
            studentEnrollment.AcademicYear,
            studentEnrollment.Semester);
        return timetableEnrollments.FirstOrDefault(enrollment =>
        {
            var departmentId = enrollment.Course?.DepartmentId;
            return departmentId.HasValue
                && enrollment.ScheduleEntry is not null
                && EnrollmentCohortKey.Create(
                    departmentId.Value,
                    enrollment.YearLevel,
                    enrollment.ScheduleEntry.Shift,
                    enrollment.AcademicYear,
                    enrollment.Semester) == studentCohort;
        })?.CourseId;
    }

    private static AcademicShift RequiredShift(string name) =>
        AcademicTimetablePolicy.FindShift(name)
        ?? throw new InvalidOperationException("Student shift is not configured in the academic timetable policy.");

    private sealed record ActiveStudentEnrollment(
        Student Student,
        Guid DepartmentId,
        int YearLevel,
        string Shift,
        string AcademicYear,
        string Semester);
}
