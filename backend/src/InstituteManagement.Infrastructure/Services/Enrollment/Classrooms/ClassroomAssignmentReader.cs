using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Classrooms;

internal sealed class ClassroomAssignmentReader(InstituteDbContext db)
{
    public async Task<PagedResult<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        PageRequest page,
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

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            assignmentQuery = assignmentQuery.Where(assignment =>
                assignment.EnrollmentCode.Contains(term)
                || assignment.Classroom!.ClassroomCode.Contains(term)
                || assignment.Classroom.Building.Contains(term)
                || assignment.Department!.Name.Contains(term)
                || assignment.Classroom.Status.Contains(term));

        var totalCount = await assignmentQuery.CountAsync(cancellationToken);
        var assignments = await assignmentQuery
            .OrderBy(assignment => assignment.Classroom!.ClassroomCode)
            .ThenBy(assignment => assignment.Id)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        if (assignments.Count == 0) return PagedResult<EnrollmentItemDto>.Create([], page, totalCount);

        var roomIds = assignments.Select(assignment => assignment.ClassroomId).Distinct().ToList();
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Include(entry => entry.Teacher)
            .Where(entry => entry.Status != "Cancelled" && entry.ClassroomId.HasValue && roomIds.Contains(entry.ClassroomId.Value))
            .ToListAsync(cancellationToken);

        var items = assignments.Select(assignment =>
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
        return PagedResult<EnrollmentItemDto>.Create(items, page, totalCount);
    }

    private static bool IsCurrent(string academicYear, string semester, EnrollmentPeriod period) =>
        academicYear == period.AcademicYear && semester == period.Semester;
}
