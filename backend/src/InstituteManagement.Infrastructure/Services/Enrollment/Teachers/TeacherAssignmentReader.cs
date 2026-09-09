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
        var teachers = await db.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.Status != "Inactive")
            .ToListAsync(cancellationToken);
        var teacherById = teachers.ToDictionary(teacher => teacher.Id);
        var teacherIds = teachers.Select(teacher => teacher.Id).ToList();
        var assignments = await db.TeacherAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Department)
            .Where(assignment => assignment.Status != "Removed" && teacherIds.Contains(assignment.TeacherId))
            .ToListAsync(cancellationToken);
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Where(entry => entry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return assignments
            .Where(assignment =>
                (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || schedules.Any(entry =>
                    entry.TeacherId == assignment.TeacherId && entry.YearLevel == year))
                && Matches(search, assignment.EnrollmentCode, teacherById[assignment.TeacherId].TeacherCode, teacherById[assignment.TeacherId].FullName, assignment.Department?.Name))
            .Select(assignment =>
            {
                var teacher = teacherById[assignment.TeacherId];
                var teacherSchedule = schedules
                    .Where(entry => entry.TeacherId == teacher.Id)
                    .ToList();
                return Item(
                    teacher.Id,
                    ("enrollmentCode", assignment.EnrollmentCode),
                    ("teacherCode", teacher.TeacherCode),
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
