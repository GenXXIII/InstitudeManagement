using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Students;

internal sealed class StudentEnrollmentEditor(
    InstituteDbContext db,
    StudentEnrollmentRecordSynchronizer recordSynchronizer,
    FinancialAccountSynchronizer financialAccountSynchronizer)
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
        if (enrollment is null)
        {
            var codes = await BusinessCodeFormatter.GenerateEnrollmentWorkflowAsync(db, student.StudentCode, "student", id, cancellationToken);
            var enrollmentId = Guid.NewGuid();
            enrollment = new StudentEnrollment
            {
                Id = enrollmentId,
                EnrollmentCode = codes.Enrollment,
                PublicId = await BusinessCodeFormatter.GenerateEnrollmentPublicIdAsync(db, enrollmentId, "studentPublicIdPrefix", "STU", cancellationToken),
                FinanceCode = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, student.StudentCode, "student", codes.Enrollment, "financeCodePrefix", "FIN", cancellationToken),
                ResultCode = await BusinessCodeFormatter.GenerateEnrollmentScopedAsync(db, student.StudentCode, "student", codes.Enrollment, "resultCodePrefix", "RES", cancellationToken),
                OperationCode = codes.Operation,
                RecordCode = codes.Record,
                HistoryCode = codes.History,
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
        enrollment.YearLevel = year;
        enrollment.Shift = shift;
        enrollment.Status = Choice(values, "status", ["Active", "Paused", "Completed"], "Active");
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        student.DepartmentId = departmentId;
        student.YearLevel = year;
        student.Shift = shift;
        var account = await financialAccountSynchronizer.EnsureForEnrollmentAsync(enrollment, student, cancellationToken);
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
            ("publicId", enrollment.PublicId),
            ("name", student.FullName),
            ("departmentId", departmentId.ToString()),
            ("year", year.ToString()),
            ("shift", shift),
            ("status", enrollment.Status),
            ("academicYear", enrollment.AcademicYear),
            ("semester", enrollment.Semester),
            ("periodState", "Current"),
            ("paymentStatus", account.Status),
            ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));
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
