using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Teachers;

internal sealed class TeacherAssignmentEditor(InstituteDbContext db, TeacherAssignmentPolicy policy)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var teacher = await db.Teachers.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Teacher not found.");
        var departmentId = await OptionalDepartmentAsync(db, values, cancellationToken);
        var assignment = await db.TeacherAssignments.FirstOrDefaultAsync(
            item =>
                item.TeacherId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
        var enrollmentCode = await BusinessCodeFormatter.FormatAsync(db, values, "enrollmentCode", "teacher", "enrollment", cancellationToken);
        if (await db.TeacherAssignments.AnyAsync(item => item.Id != (assignment == null ? Guid.Empty : assignment.Id) && item.EnrollmentCode == enrollmentCode, cancellationToken))
            throw new InvalidOperationException("EnrollmentCode already exists.");

        if (assignment is null)
        {
            assignment = new TeacherAssignment
            {
                TeacherId = id,
                AcademicYear = period.AcademicYear,
                Semester = period.Semester
            };
            db.TeacherAssignments.Add(assignment);
        }

        if (assignment.DepartmentId != departmentId)
        {
            await policy.EnsureDepartmentCanChangeAsync(id, departmentId, period, cancellationToken);
        }

        assignment.DepartmentId = departmentId;
        assignment.EnrollmentCode = enrollmentCode;
        assignment.Status = Choice(
            values,
            "status",
            ["Assigned", "On leave", "Unassigned"],
            departmentId.HasValue ? "Assigned" : "Unassigned");
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        teacher.DepartmentId = departmentId;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Teacher",
            teacher.TeacherCode,
            "Assignment updated",
            values));

        return Item(
            id,
            ("enrollmentCode", assignment.EnrollmentCode),
            ("teacherCode", teacher.TeacherCode),
            ("name", teacher.FullName),
            ("departmentId", departmentId?.ToString() ?? ""),
            ("status", assignment.Status));
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignment = await db.TeacherAssignments.FirstOrDefaultAsync(
            item =>
                item.TeacherId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);

        if (assignment is null || assignment.Status == "Removed")
        {
            return false;
        }

        await policy.EnsureCanRemoveAsync(id, period, cancellationToken);
        var teacher = await db.Teachers.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Teacher not found.");
        var values = AssignmentValues(
            ("departmentId", assignment.DepartmentId?.ToString() ?? ""),
            ("status", assignment.Status),
            ("academicYear", assignment.AcademicYear),
            ("semester", assignment.Semester));

        assignment.Status = "Removed";
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        teacher.DepartmentId = null;
        teacher.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Teacher",
            teacher.TeacherCode,
            "Assignment removed",
            values));
        return true;
    }

}
