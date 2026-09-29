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
            var latestEnrollment = await db.StudentEnrollments.AsNoTracking()
                .Where(item => item.StudentId == id)
                .OrderByDescending(item => item.CreateAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var enrollmentCode = latestEnrollment is not null && latestEnrollment.Status != "Removed"
                ? latestEnrollment.EnrollmentCode
                : (await BusinessCodeFormatter.GenerateEnrollmentWorkflowAsync(db, student.StudentCode, "student", id, cancellationToken)).Enrollment;
            var periodCodes = await BusinessCodeFormatter.GenerateStudentPeriodWorkflowAsync(db, student.StudentCode, enrollmentCode, year, period.Semester, cancellationToken);
            var academicEnrollment = await StudentAcademicEnrollmentResolver.GetOrCreateAsync(
                db,
                student.Id,
                periodCodes.Enrollment,
                cancellationToken);
            var enrollmentId = Guid.NewGuid();
            var publicId = latestEnrollment?.EnrollmentCode == periodCodes.Enrollment
                && !string.IsNullOrWhiteSpace(latestEnrollment.PublicId)
                ? latestEnrollment.PublicId
                : await BusinessCodeFormatter.GenerateEnrollmentPublicIdAsync(db, enrollmentId, "studentPublicIdPrefix", "STU", cancellationToken);
            enrollment = new StudentEnrollment
            {
                Id = enrollmentId,
                StudentAcademicEnrollmentId = academicEnrollment.Id,
                StudentAcademicEnrollment = academicEnrollment,
                EnrollmentCode = periodCodes.Enrollment,
                PublicId = publicId,
                FinanceCode = await BusinessCodeFormatter.GenerateStudentPeriodScopedAsync(db, periodCodes.Enrollment, year, period.Semester, "financeCodePrefix", "FIN", cancellationToken),
                ResultCode = await BusinessCodeFormatter.GenerateStudentPeriodScopedAsync(db, periodCodes.Enrollment, year, period.Semester, "resultCodePrefix", "RES", cancellationToken),
                OperationCode = periodCodes.Operation,
                RecordCode = periodCodes.Record,
                HistoryCode = periodCodes.History,
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
        var academicEnrollment = await StudentAcademicEnrollmentResolver.GetOrCreateAsync(
            db,
            student.Id,
            enrollment.EnrollmentCode,
            cancellationToken);
        academicEnrollment.Status = "Cancelled";
        academicEnrollment.CompletedAtUtc = DateTime.UtcNow;
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
