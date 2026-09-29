using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Classrooms;

internal sealed class ClassroomAssignmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignmentQuery = db.ClassroomAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Classroom)
            .Include(assignment => assignment.Department)
            .Where(assignment =>
                assignment.Status != "Removed"
                && assignment.Classroom != null
                && assignment.Classroom.Status != "Inactive"
                && (!departmentId.HasValue
                    || assignment.DepartmentId == departmentId
                    || assignment.DepartmentId == null));
        if (year.HasValue)
        {
            assignmentQuery = assignmentQuery.Where(assignment => db.ScheduleEntries.Any(entry =>
                entry.Status != "Cancelled"
                && entry.ClassroomId == assignment.ClassroomId
                && entry.YearLevel == year));
        }

        var assignments = await assignmentQuery
            .ToListAsync(cancellationToken);
        if (assignments.Count == 0) return [];

        var roomIds = assignments.Select(assignment => assignment.ClassroomId).Distinct().ToList();
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Include(entry => entry.Teacher)
            .Where(entry => entry.Status != "Cancelled" && entry.ClassroomId.HasValue && roomIds.Contains(entry.ClassroomId.Value))
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                Matches(
                    search,
                    assignment.EnrollmentCode,
                    assignment.Classroom!.ClassroomCode,
                    assignment.Classroom.Building,
                    assignment.Department?.Name,
                    assignment.Classroom.Status))
            .Select(assignment =>
            {
                var room = assignment.Classroom!;
                var roomSchedule = schedules
                    .Where(entry =>
                        entry.ClassroomId == room.Id
                        && (!year.HasValue || entry.YearLevel == year))
                    .ToList();
                return Item(
                    room.Id,
                    ("enrollmentCode", assignment.EnrollmentCode),
                    ("classroomCode", room.ClassroomCode),
                    ("building", room.Building),
                    ("roomType", room.RoomType),
                    ("departmentId", assignment.DepartmentId?.ToString() ?? ""),
                    ("department", assignment.Department?.Name ?? "Shared institute"),
                    ("capacity", assignment.Capacity.ToString()),
                    ("access", assignment.Access),
                    ("status", assignment.Status),
                    ("courses", string.Join(", ", roomSchedule.Select(entry => entry.Course?.Name).Where(name => name is not null).Distinct())),
                    ("teachers", string.Join(", ", roomSchedule.Select(entry => entry.Teacher?.FullName).Where(name => name is not null).Distinct())),
                    ("yearLevels", string.Join(", ", roomSchedule.Select(entry => entry.YearLevel).Distinct().Order().Select(value => $"Year {value}"))),
                    ("weeklyClasses", roomSchedule.Count.ToString()),
                    ("academicYear", assignment.AcademicYear),
                    ("semester", assignment.Semester),
                    ("periodState", IsCurrent(assignment.AcademicYear, assignment.Semester, period) ? "Current" : "Retained"),
                    ("createAt", assignment.CreateAt.ToString("yyyy-MM-dd")));
            })
            .ToList();
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;
}
