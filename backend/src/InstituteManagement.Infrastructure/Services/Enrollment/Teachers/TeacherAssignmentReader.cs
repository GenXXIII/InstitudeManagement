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
        var rows = await db.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.Status != "Inactive")
            .GroupJoin(
                db.TeacherAssignments
                    .AsNoTracking()
                    .Where(assignment =>
                        assignment.AcademicYear == period.AcademicYear
                        && assignment.Semester == period.Semester
                        && assignment.Status != "Removed"),
                teacher => teacher.Id,
                assignment => assignment.TeacherId,
                (teacher, assignments) => new
                {
                    teacher,
                    assignment = assignments.FirstOrDefault()
                })
            .Select(row => new
            {
                row.teacher,
                row.assignment,
                department = row.assignment == null ? null : row.assignment.Department
            })
            .ToListAsync(cancellationToken);
        var schedules = await db.ScheduleEntries
            .AsNoTracking()
            .Include(entry => entry.Course)
            .Where(entry => entry.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        return rows
            .Where(row =>
                (!departmentId.HasValue || row.assignment?.DepartmentId == departmentId)
                && (!year.HasValue || schedules.Any(entry =>
                    entry.TeacherId == row.teacher.Id && entry.YearLevel == year))
                && Matches(search, row.assignment?.EnrollmentCode, row.teacher.TeacherCode, row.teacher.FullName, row.department?.Name))
            .Select(row =>
            {
                var teacherSchedule = schedules
                    .Where(entry => entry.TeacherId == row.teacher.Id)
                    .ToList();
                return Item(
                    row.teacher.Id,
                    ("enrollmentCode", row.assignment?.EnrollmentCode ?? ""),
                    ("teacherCode", row.teacher.TeacherCode),
                    ("name", row.teacher.FullName),
                    ("email", row.teacher.Email),
                    ("photoDataUrl", row.teacher.PhotoDataUrl),
                    ("departmentId", row.assignment?.DepartmentId.ToString() ?? ""),
                    ("department", row.department?.Name ?? "Unassigned"),
                    ("status", row.assignment?.Status ?? "Unassigned"),
                    ("courseCount", teacherSchedule.Select(entry => entry.CourseId).Distinct().Count().ToString()),
                    ("courses", string.Join(", ", teacherSchedule.Select(entry => entry.Course?.Name).Where(name => name is not null).Distinct())),
                    ("yearLevels", string.Join(", ", teacherSchedule.Select(entry => entry.YearLevel).Distinct().Order().Select(value => $"Year {value}"))),
                    ("weeklyClasses", teacherSchedule.Count.ToString()),
                    ("learningSpaces", teacherSchedule.Select(entry => entry.ClassroomId).Distinct().Count().ToString()),
                    ("academicYear", period.AcademicYear),
                    ("semester", period.Semester));
            })
            .ToList();
    }
}
