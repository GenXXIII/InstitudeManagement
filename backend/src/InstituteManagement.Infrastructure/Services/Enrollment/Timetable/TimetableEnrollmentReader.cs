using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignments = await db.CourseAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Where(assignment =>
                assignment.AcademicYear == period.AcademicYear
                && assignment.Semester == period.Semester
                && assignment.Status == "Active")
            .ToDictionaryAsync(assignment => assignment.CourseId, cancellationToken);
        var enrolledIds = await db.TimetableEnrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.AcademicYear == period.AcademicYear
                && enrollment.Semester == period.Semester
                && enrollment.Status == "Active")
            .Select(enrollment => enrollment.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var entries = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Include(entry => entry.Teacher)
            .Include(entry => entry.Classroom)
            .Where(entry => entry.Status != "Cancelled" && enrolledIds.Contains(entry.Id))
            .ToListAsync(cancellationToken);

        return entries
            .Where(entry =>
                assignments.TryGetValue(entry.CourseId, out var assignment)
                && (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || entry.YearLevel == year)
                && Matches(
                    search,
                    entry.TimetableCode,
                    entry.Course?.CourseCode,
                    entry.Course?.Name,
                    entry.Teacher?.TeacherCode,
                    entry.Teacher?.FullName,
                    entry.Classroom?.ClassroomCode,
                    assignment.Department?.Name))
            .Select(entry =>
            {
                var assignment = assignments[entry.CourseId];
                return TimetableEnrollmentItemFactory.Create(
                    entry,
                    assignment.DepartmentId,
                    assignment.Department?.Name);
            })
            .ToList();
    }
}
