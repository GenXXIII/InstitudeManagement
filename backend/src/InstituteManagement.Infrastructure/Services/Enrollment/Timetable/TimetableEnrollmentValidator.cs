using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed record ValidatedTimetableAssignment(
    Course Course,
    Teacher Teacher,
    Classroom Classroom,
    Guid? DepartmentId,
    string? DepartmentName);

internal sealed class TimetableEnrollmentValidator(InstituteDbContext db)
{
    public async Task<ValidatedTimetableAssignment> ValidateAsync(
        ScheduleEntry schedule,
        Guid courseId,
        Guid teacherId,
        Guid classroomId,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Include(item => item.Department)
            .FirstOrDefaultAsync(item => item.Id == courseId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Select an active course from Management.");
        if (!course.Semester.Equals(period.Semester, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"This Management course is assigned to {course.Semester}, not {period.Semester}.");

        var teacher = await db.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == teacherId && item.Status != "Inactive", cancellationToken)
            ?? throw new InvalidOperationException("Select an active teacher from Management.");

        var classroom = await db.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == classroomId && item.Status == "Available", cancellationToken)
            ?? throw new InvalidOperationException("Select an Available classroom from Management.");
        if (classroom.Capacity < course.Capacity)
            throw new InvalidOperationException("Management classroom capacity must cover the Management course capacity.");

        ValidateClassroomYear(course.YearLevel, classroom.ClassroomCode);
        if (await db.TimetableEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.ScheduleEntry)
            .AnyAsync(enrollment =>
                enrollment.ScheduleEntryId != schedule.Id
                && enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active"
                && enrollment.ScheduleEntry != null
                && enrollment.ScheduleEntry.DayOfWeek == schedule.DayOfWeek
                && enrollment.ScheduleEntry.StartsAt < schedule.EndsAt
                && schedule.StartsAt < enrollment.ScheduleEntry.EndsAt
                && (enrollment.TeacherId == teacherId || enrollment.ClassroomId == classroomId),
                cancellationToken))
        {
            throw new InvalidOperationException("Teacher or classroom is already enrolled during this time.");
        }

        return new ValidatedTimetableAssignment(
            course,
            teacher,
            classroom,
            course.DepartmentId,
            course.Department?.Name);
    }

    private static void ValidateClassroomYear(int yearLevel, string? classroomCode)
    {
        if (yearLevel == 1 && classroomCode != "501")
            throw new InvalidOperationException("Year 1 must use Classroom 501.");
        if (yearLevel >= 2 && classroomCode == "501")
            throw new InvalidOperationException("Classroom 501 is reserved for Year 1.");
    }
}
