using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Classrooms;

internal sealed class ClassroomAssignmentEditor(InstituteDbContext db)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var room = await db.Classrooms.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Classroom not found.");
        var departmentId = await OptionalDepartmentAsync(db, values, cancellationToken);
        var capacity = Integer(values, "capacity", 1, 10000);
        var assignment = await db.ClassroomAssignments.FirstOrDefaultAsync(
            item =>
                item.ClassroomId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
        var enrollmentCode = await BusinessCodeFormatter.FormatAsync(db, values, "enrollmentCode", "classroom", "enrollment", cancellationToken);
        if (await db.ClassroomAssignments.AnyAsync(item => item.Id != (assignment == null ? Guid.Empty : assignment.Id) && item.EnrollmentCode == enrollmentCode, cancellationToken))
            throw new InvalidOperationException("EnrollmentCode already exists.");

        if (assignment is null)
        {
            assignment = new ClassroomAssignment
            {
                ClassroomId = id,
                AcademicYear = period.AcademicYear,
                Semester = period.Semester
            };
            db.ClassroomAssignments.Add(assignment);
        }

        assignment.DepartmentId = departmentId;
        assignment.EnrollmentCode = enrollmentCode;
        assignment.Capacity = capacity;
        assignment.Access = Choice(
            values,
            "access",
            ["Shared institute", "Department only"],
            "Shared institute");
        assignment.Status = Choice(values, "status", ["Available", "Maintenance"], "Available");
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        room.DepartmentId = departmentId;
        room.Capacity = capacity;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Classroom",
            room.ClassroomCode,
            "Assignment updated",
            values));

        return Item(
            id,
            ("enrollmentCode", assignment.EnrollmentCode),
            ("classroomCode", room.ClassroomCode),
            ("building", room.Building),
            ("departmentId", departmentId?.ToString() ?? ""),
            ("capacity", capacity.ToString()),
            ("access", assignment.Access),
            ("status", assignment.Status));
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var assignment = await db.ClassroomAssignments.FirstOrDefaultAsync(
            item =>
                item.ClassroomId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);

        if (assignment is null || assignment.Status == "Removed")
        {
            return false;
        }

        if (await db.ScheduleEntries.AnyAsync(
                entry => entry.ClassroomId == id && entry.Status != "Cancelled",
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Remove this learning space's active timetable relationships first.");
        }

        var room = await db.Classrooms.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Classroom not found.");
        var values = AssignmentValues(
            ("departmentId", assignment.DepartmentId?.ToString() ?? ""),
            ("capacity", assignment.Capacity.ToString()),
            ("access", assignment.Access),
            ("status", assignment.Status),
            ("academicYear", assignment.AcademicYear),
            ("semester", assignment.Semester));

        assignment.Status = "Removed";
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        room.DepartmentId = null;
        room.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Classroom",
            room.ClassroomCode,
            "Assignment removed",
            values));
        return true;
    }
}
