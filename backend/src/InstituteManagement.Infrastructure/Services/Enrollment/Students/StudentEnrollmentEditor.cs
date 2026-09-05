using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal sealed class StudentEnrollmentEditor(
    InstituteDbContext db,
    StudentEnrollmentRecordSynchronizer recordSynchronizer)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Student not found.");
        var departmentId = await RequiredDepartmentAsync(db, values, cancellationToken);
        var year = Integer(values, "year", 1, 4);
        var shift = Choice(values, "shift", AcademicTimetablePolicy.ShiftNames);
        var enrollment = await db.StudentEnrollments.FirstOrDefaultAsync(
            item =>
                item.StudentId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
        var enrollmentCode = await BusinessCodeFormatter.FormatAsync(db, values, "enrollmentCode", "student", "enrollment", cancellationToken);
        if (await db.StudentEnrollments.AnyAsync(item => item.Id != (enrollment == null ? Guid.Empty : enrollment.Id) && item.EnrollmentCode == enrollmentCode, cancellationToken))
            throw new InvalidOperationException("EnrollmentCode already exists.");

        if (enrollment is null)
        {
            enrollment = new StudentEnrollment
            {
                StudentId = id,
                AcademicYear = period.AcademicYear,
                Semester = period.Semester
            };
            db.StudentEnrollments.Add(enrollment);
        }

        if (enrollment.DepartmentId != departmentId
            || enrollment.YearLevel != year
            || enrollment.Shift != shift)
        {
            await recordSynchronizer.ReassignAsync(
                student,
                departmentId,
                year,
                shift,
                period,
                cancellationToken);
        }

        enrollment.DepartmentId = departmentId;
        enrollment.EnrollmentCode = enrollmentCode;
        enrollment.YearLevel = year;
        enrollment.Shift = shift;
        enrollment.Status = Choice(values, "status", ["Active", "Paused", "Completed"], "Active");
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        student.DepartmentId = departmentId;
        student.YearLevel = year;
        student.Shift = shift;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Student",
            student.StudentCode,
            "Enrollment updated",
            values));

        return Item(
            id,
            ("enrollmentCode", enrollment.EnrollmentCode),
            ("studentCode", student.StudentCode),
            ("name", student.FullName),
            ("departmentId", departmentId.ToString()),
            ("year", year.ToString()),
            ("shift", shift),
            ("status", enrollment.Status));
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var enrollment = await db.StudentEnrollments.FirstOrDefaultAsync(
            item =>
                item.StudentId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);

        if (enrollment is null || enrollment.Status == "Removed")
        {
            return false;
        }

        var student = await db.Students.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Student not found.");
        var values = AssignmentValues(
            ("departmentId", enrollment.DepartmentId.ToString()),
            ("year", enrollment.YearLevel.ToString()),
            ("shift", enrollment.Shift),
            ("status", enrollment.Status),
            ("academicYear", enrollment.AcademicYear),
            ("semester", enrollment.Semester));

        enrollment.Status = "Removed";
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        student.DepartmentId = null;
        student.YearLevel = 0;
        student.Shift = "";
        student.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Student",
            student.StudentCode,
            "Enrollment removed",
            values));
        return true;
    }
}
