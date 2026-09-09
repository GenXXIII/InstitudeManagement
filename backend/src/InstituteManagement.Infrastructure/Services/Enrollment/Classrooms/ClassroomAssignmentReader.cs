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
        var rooms = await db.Classrooms
            .AsNoTracking()
            .Where(room => room.Status != "Inactive")
            .ToListAsync(cancellationToken);
        var roomById = rooms.ToDictionary(room => room.Id);
        var roomIds = rooms.Select(room => room.Id).ToList();
        var assignments = await db.ClassroomAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Where(assignment => assignment.Status != "Removed" && roomIds.Contains(assignment.ClassroomId))
            .ToListAsync(cancellationToken);
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Include(entry => entry.Teacher)
            .Where(entry => entry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                (!departmentId.HasValue
                    || assignment.DepartmentId == departmentId
                    || assignment.DepartmentId == null)
                && (!year.HasValue || schedules.Any(entry =>
                    entry.ClassroomId == assignment.ClassroomId && entry.YearLevel == year))
                && Matches(search, assignment.EnrollmentCode, roomById[assignment.ClassroomId].ClassroomCode, roomById[assignment.ClassroomId].Building, assignment.Department?.Name, roomById[assignment.ClassroomId].Status))
            .Select(assignment =>
            {
                var room = roomById[assignment.ClassroomId];
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
