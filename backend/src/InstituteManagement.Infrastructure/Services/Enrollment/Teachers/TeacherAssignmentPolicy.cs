using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Teachers;

internal sealed class TeacherAssignmentPolicy(InstituteDbContext db)
{
    public async Task EnsureDepartmentCanChangeAsync(
        Guid teacherId,
        Guid? departmentId,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var hasCourseConflict = await db.CourseAssignments.AnyAsync(
            item => item.TeacherId == teacherId
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester
                && item.Status == "Active"
                && (!departmentId.HasValue || item.DepartmentId != departmentId),
            cancellationToken);
        var headsOtherDepartment = await db.Departments.AnyAsync(
            department => department.HeadTeacherId == teacherId
                && (!departmentId.HasValue || department.Id != departmentId),
            cancellationToken);

        if (hasCourseConflict || headsOtherDepartment)
            throw new InvalidOperationException(
                "Reassign this teacher's active course and department-head assignments first.");
    }

    public async Task EnsureCanRemoveAsync(
        Guid teacherId,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var hasRelationships = await db.CourseAssignments.AnyAsync(
                item => item.TeacherId == teacherId
                    && item.AcademicYear == period.AcademicYear
                    && item.Semester == period.Semester
                    && item.Status != "Removed",
                cancellationToken)
            || await db.ScheduleEntries.AnyAsync(
                entry => entry.TeacherId == teacherId && entry.Status != "Cancelled",
                cancellationToken)
            || await db.Departments.AnyAsync(
                department => department.HeadTeacherId == teacherId,
                cancellationToken);

        if (hasRelationships)
            throw new InvalidOperationException(
                "Remove this teacher's active course, timetable, and department-head relationships first.");
    }
}
