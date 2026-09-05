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
        var rows = await db.Classrooms
            .AsNoTracking()
            .Where(room => room.Status != "Inactive")
            .GroupJoin(
                db.ClassroomAssignments
                    .AsNoTracking()
                    .Where(assignment =>
                        assignment.AcademicYear == period.AcademicYear
                        && assignment.Semester == period.Semester
                        && assignment.Status != "Removed"),
                room => room.Id,
                assignment => assignment.ClassroomId,
                (room, assignments) => new
                {
                    room,
                    assignment = assignments.FirstOrDefault()
                })
            .Select(row => new
            {
                row.room,
                row.assignment,
                department = row.assignment == null ? null : row.assignment.Department
            })
            .ToListAsync(cancellationToken);
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Include(entry => entry.Teacher)
            .Where(entry => entry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return rows
            .Where(row =>
                (!departmentId.HasValue
                    || row.assignment?.DepartmentId == departmentId
                    || row.assignment?.DepartmentId == null)
                && (!year.HasValue || schedules.Any(entry =>
                    entry.ClassroomId == row.room.Id && entry.YearLevel == year))
                && Matches(search, row.assignment?.EnrollmentCode, row.room.ClassroomCode, row.room.Building, row.department?.Name, row.room.Status))
            .Select(row =>
            {
                var roomSchedule = schedules
                    .Where(entry =>
                        entry.ClassroomId == row.room.Id
                        && (!year.HasValue || entry.YearLevel == year))
                    .ToList();
                return Item(
                    row.room.Id,
                    ("enrollmentCode", row.assignment?.EnrollmentCode ?? ""),
                    ("classroomCode", row.room.ClassroomCode),
                    ("building", row.room.Building),
                    ("roomType", row.room.RoomType),
                    ("departmentId", row.assignment?.DepartmentId.ToString() ?? ""),
                    ("department", row.department?.Name ?? "Shared institute"),
                    ("capacity", row.assignment?.Capacity.ToString() ?? row.room.Capacity.ToString()),
                    ("access", row.assignment?.Access ?? "Shared institute"),
                    ("status", row.room.Status),
                    ("courses", string.Join(", ", roomSchedule.Select(entry => entry.Course?.Name).Where(name => name is not null).Distinct())),
                    ("teachers", string.Join(", ", roomSchedule.Select(entry => entry.Teacher?.FullName).Where(name => name is not null).Distinct())),
                    ("yearLevels", string.Join(", ", roomSchedule.Select(entry => entry.YearLevel).Distinct().Order().Select(value => $"Year {value}"))),
                    ("weeklyClasses", roomSchedule.Count.ToString()),
                    ("academicYear", period.AcademicYear),
                    ("semester", period.Semester));
            })
            .ToList();
    }
}
