using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Teachers;

internal sealed class TeacherAssignmentReader(InstituteDbContext db)
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(
        string? search,
        Guid? departmentId,
        int? year,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignmentQuery = db.TeacherAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Include(assignment => assignment.Teacher)
            .Where(assignment =>
                assignment.Status != "Removed"
                && assignment.Teacher != null
                && assignment.Teacher.Status != "Inactive"
                && (!departmentId.HasValue || assignment.DepartmentId == departmentId));
        if (year.HasValue)
        {
            assignmentQuery = assignmentQuery.Where(assignment => db.ScheduleEntries.Any(entry =>
                entry.Status != "Cancelled"
                && entry.TeacherId == assignment.TeacherId
                && entry.YearLevel == year));
        }

        var assignments = await assignmentQuery
            .ToListAsync(cancellationToken);
        if (assignments.Count == 0) return [];

        var teacherIds = assignments.Select(assignment => assignment.TeacherId).Distinct().ToList();
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Where(entry => entry.Status != "Cancelled" && entry.TeacherId.HasValue && teacherIds.Contains(entry.TeacherId.Value))
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                Matches(
                    search,
                    assignment.EnrollmentCode,
                    assignment.Teacher!.TeacherCode,
                    assignment.Teacher.FullName,
                    assignment.Department?.Name))
            .Select(assignment =>
            {
                var teacher = assignment.Teacher!;
                var teacherSchedule = schedules
                    .Where(entry => entry.TeacherId == teacher.Id)
                    .ToList();
                return Item(
                    teacher.Id,
                    ("enrollmentCode", assignment.EnrollmentCode),
                    ("teacherCode", teacher.TeacherCode),
                    ("publicId", assignment.PublicId),
                    ("name", teacher.FullName),
                    ("email", teacher.Email),
                    ("photoDataUrl", teacher.PhotoDataUrl),
                    ("departmentId", assignment.DepartmentId?.ToString() ?? ""),
                    ("department", assignment.Department?.Name ?? "Unassigned"),
                    ("status", assignment.Status),
                    ("courseCount", teacherSchedule.Select(entry => entry.CourseId).Distinct().Count().ToString()),
                    ("courses", string.Join(", ", teacherSchedule.Select(entry => entry.Course?.Name).Where(name => name is not null).Distinct())),
                    ("yearLevels", string.Join(", ", teacherSchedule.Select(entry => entry.YearLevel).Distinct().Order().Select(value => $"Year {value}"))),
                    ("weeklyClasses", teacherSchedule.Count.ToString()),
                    ("learningSpaces", teacherSchedule.Select(entry => entry.ClassroomId).Distinct().Count().ToString()),
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
