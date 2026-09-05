using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentValidator(InstituteDbContext db)
{
    public async Task<CourseAssignment> ValidateAsync(
        ScheduleEntry entry,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var course = await db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Course)
            .Include(assignment => assignment.Department)
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.CourseId == entry.CourseId
                    && assignment.AcademicYear == period.AcademicYear
                    && assignment.Semester == period.Semester
                    && assignment.Status == "Active",
                cancellationToken)
            ?? throw new InvalidOperationException("Assign this course in Course Assign first.");
        if (course.YearLevel != entry.YearLevel || course.TeacherId != entry.TeacherId)
        {
            throw new InvalidOperationException(
                "The Management schedule must match the current course year and teacher assignment.");
        }

        var teacher = await db.TeacherAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.TeacherId == entry.TeacherId
                    && assignment.AcademicYear == period.AcademicYear
                    && assignment.Semester == period.Semester
                    && assignment.Status == "Assigned",
                cancellationToken)
            ?? throw new InvalidOperationException("Assign this teacher in Teacher Assign first.");
        if (teacher.DepartmentId.HasValue && teacher.DepartmentId != course.DepartmentId)
        {
            throw new InvalidOperationException(
                "Teacher and course must belong to the same enrollment department.");
        }

        var classroom = await db.ClassroomAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Classroom)
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.ClassroomId == entry.ClassroomId
                    && assignment.AcademicYear == period.AcademicYear
                    && assignment.Semester == period.Semester
                    && assignment.Status != "Removed",
                cancellationToken)
            ?? throw new InvalidOperationException(
                "This classroom does not have a current-semester assignment.");
        if (classroom.DepartmentId.HasValue && classroom.DepartmentId != course.DepartmentId)
        {
            throw new InvalidOperationException("This classroom is assigned to another department.");
        }
        if (classroom.Capacity < course.Capacity)
        {
            throw new InvalidOperationException(
                "Classroom assignment capacity must cover the course capacity.");
        }

        TimetableScheduleEditor.ValidateClassroomYear(
            entry.YearLevel,
            classroom.Classroom?.ClassroomCode);
        return course;
    }
}
