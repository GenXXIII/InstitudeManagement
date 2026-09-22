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
        if (assignment is null)
        {
            var codes = await BusinessCodeFormatter.GenerateEnrollmentWorkflowAsync(db, teacher.TeacherCode, "teacher", id, cancellationToken);
            assignment = new TeacherAssignment
            {
                EnrollmentCode = codes.Enrollment,
                PublicId = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, teacher.TeacherCode, "teacher", codes.Enrollment, "teacherPublicIdPrefix", "TID", cancellationToken),
                OperationCode = codes.Operation,
                RecordCode = codes.Record,
                HistoryCode = codes.History,
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
            ("publicId", assignment.PublicId),
            ("name", teacher.FullName),
            ("departmentId", departmentId?.ToString() ?? ""),
            ("status", assignment.Status),
            ("academicYear", assignment.AcademicYear),
            ("semester", assignment.Semester),
            ("periodState", "Current"),
            ("createAt", assignment.CreateAt.ToString("yyyy-MM-dd")));
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
