using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal sealed class StudentEnrollmentRecordSynchronizer(InstituteDbContext db)
{
    public async Task ReassignAsync(
        Student student,
        Guid departmentId,
        int year,
        string shift,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var courseIds = await db.CourseAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.DepartmentId == departmentId
                && assignment.YearLevel == year
                && assignment.AcademicYear == period.AcademicYear
                && assignment.Semester == period.Semester
                && assignment.Status == "Active")
            .Select(assignment => assignment.CourseId)
            .ToListAsync(cancellationToken);
        var courseId = await db.ScheduleEntries
                .AsNoTracking()
                .Where(entry => courseIds.Contains(entry.CourseId) && entry.Status != "Cancelled")
                .OrderBy(entry => entry.TimetableCode)
                .Select(entry => (Guid?)entry.CourseId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? courseIds.FirstOrDefault();

        if (courseId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Assign a course for the selected department and year before saving this student enrollment.");
        }

        var grades = await db.GradeRecords
            .Where(grade =>
                grade.StudentId == student.Id
                && grade.AcademicYear == period.AcademicYear
                && grade.Term == period.Semester)
            .ToListAsync(cancellationToken);
        foreach (var grade in grades)
        {
            grade.CourseId = courseId;
            grade.Score = 0;
            grade.LetterGrade = "F";
            grade.UpdatedAtUtc = DateTime.UtcNow;
        }

        var startsAt = AcademicTimetablePolicy.FindShift(shift)?.StartsAt
            ?? throw new ArgumentException("Shift is invalid.");
        var attendanceRecords = await db.AttendanceRecords
            .Where(attendance =>
                attendance.StudentId == student.Id
                && attendance.AcademicYear == period.AcademicYear
                && attendance.Term == period.Semester)
            .ToListAsync(cancellationToken);
        foreach (var attendance in attendanceRecords)
        {
            attendance.CheckedInAt = startsAt;
            attendance.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
