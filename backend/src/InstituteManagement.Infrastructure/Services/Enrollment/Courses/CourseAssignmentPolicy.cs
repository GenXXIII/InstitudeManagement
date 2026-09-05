using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Courses;

internal sealed class CourseAssignmentPolicy(InstituteDbContext db, EnrollmentSettingsReader settings)
{
    public async Task<Guid?> TeacherIdAsync(
        Dictionary<string, string> values,
        Guid departmentId,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var teacherRequired = await settings.EnabledAsync(
            "courses",
            "requireAssignedTeacher",
            true,
            cancellationToken);
        var teacherId = GuidValue(values, "teacherId", teacherRequired);
        if (!teacherId.HasValue)
        {
            return null;
        }

        var teacherAssignment = await db.TeacherAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.TeacherId == teacherId.Value
                    && assignment.AcademicYear == period.AcademicYear
                    && assignment.Semester == period.Semester
                    && assignment.Status == "Assigned",
                cancellationToken)
            ?? throw new InvalidOperationException("Assign this teacher in Teacher enrollment first.");
        var allowCrossDepartment = await settings.EnabledAsync(
            "departments",
            "allowCrossDepartmentTeaching",
            false,
            cancellationToken);
        if (!allowCrossDepartment
            && teacherAssignment.DepartmentId.HasValue
            && teacherAssignment.DepartmentId != departmentId)
        {
            throw new InvalidOperationException(
                "Course and teacher must belong to the same enrollment department unless cross-department teaching is enabled in Administration.");
        }

        return teacherId;
    }

    public async Task<int> CapacityAsync(
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var configuredCapacity = await settings.IntegerAsync(
            "courses",
            "defaultCapacity",
            40,
            1,
            10000,
            cancellationToken);
        return string.IsNullOrWhiteSpace(values.GetValueOrDefault("capacity"))
            ? configuredCapacity
            : Integer(values, "capacity", 1, 10000);
    }
}
