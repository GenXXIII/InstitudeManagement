using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Courses;

internal sealed class CourseAssignmentEditor(InstituteDbContext db, CourseAssignmentPolicy policy)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var course = await db.Courses.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");
        var departmentId = await RequiredDepartmentAsync(db, values, cancellationToken);
        var teacherId = await policy.TeacherIdAsync(values, departmentId, period, cancellationToken);
        var year = Integer(values, "year", 1, 4);
        var capacity = await policy.CapacityAsync(values, cancellationToken);
        var assignment = await db.CourseAssignments.FirstOrDefaultAsync(
            item =>
                item.CourseId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
        var enrollmentCode = await BusinessCodeFormatter.FormatAsync(db, values, "enrollmentCode", "course", "enrollment", cancellationToken);
        if (await db.CourseAssignments.AnyAsync(item => item.Id != (assignment == null ? Guid.Empty : assignment.Id) && item.EnrollmentCode == enrollmentCode, cancellationToken))
            throw new InvalidOperationException("EnrollmentCode already exists.");

        if (assignment is null)
        {
            assignment = new CourseAssignment
            {
                CourseId = id,
                AcademicYear = period.AcademicYear,
                Semester = period.Semester
            };
            db.CourseAssignments.Add(assignment);
        }

        assignment.DepartmentId = departmentId;
        assignment.EnrollmentCode = enrollmentCode;
        assignment.TeacherId = teacherId;
        assignment.YearLevel = year;
        assignment.Capacity = capacity;
        assignment.Status = Choice(values, "status", ["Active", "Paused"], "Active");
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        course.DepartmentId = departmentId;
        course.TeacherId = teacherId;
        course.Capacity = capacity;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Course",
            course.CourseCode,
            "Assignment updated",
            values));

        return Item(
            id,
            ("enrollmentCode", assignment.EnrollmentCode),
            ("courseCode", course.CourseCode),
            ("name", course.Name),
            ("departmentId", departmentId.ToString()),
            ("teacherId", teacherId?.ToString() ?? ""),
            ("year", year.ToString()),
            ("capacity", capacity.ToString()),
            ("status", assignment.Status));
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignment = await db.CourseAssignments.FirstOrDefaultAsync(
            item =>
                item.CourseId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);

        if (assignment is null || assignment.Status == "Removed")
        {
            return false;
        }

        if (await db.ScheduleEntries.AnyAsync(
                entry => entry.CourseId == id && entry.Status != "Cancelled",
                cancellationToken))
        {
            throw new InvalidOperationException("Remove this course's active timetable relationships first.");
        }

        var course = await db.Courses.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");
        var values = AssignmentValues(
            ("departmentId", assignment.DepartmentId.ToString()),
            ("teacherId", assignment.TeacherId?.ToString() ?? ""),
            ("year", assignment.YearLevel.ToString()),
            ("capacity", assignment.Capacity.ToString()),
            ("status", assignment.Status),
            ("academicYear", assignment.AcademicYear),
            ("semester", assignment.Semester));

        assignment.Status = "Removed";
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        course.DepartmentId = null;
        course.TeacherId = null;
        course.Capacity = 0;
        course.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Course",
            course.CourseCode,
            "Assignment removed",
            values));
        return true;
    }
}
