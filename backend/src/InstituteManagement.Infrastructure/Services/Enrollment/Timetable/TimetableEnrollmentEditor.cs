using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentEditor(
    InstituteDbContext db,
    TimetableEnrollmentValidator validator)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var entry = await db.ScheduleEntries.FirstOrDefaultAsync(
            item => item.Id == id && item.Status != "Cancelled",
            cancellationToken)
            ?? throw new KeyNotFoundException("Management schedule not found.");
        var courseId = GuidValue(values, "courseId", true)!.Value;
        var teacherId = GuidValue(values, "teacherId", true)!.Value;
        var classroomId = GuidValue(values, "classroomId", true)!.Value;
        var validated = await validator.ValidateAsync(
            entry,
            courseId,
            teacherId,
            classroomId,
            period,
            cancellationToken);
        var yearLevel = validated.Course.YearLevel;
        var enrollment = await db.TimetableEnrollments.FirstOrDefaultAsync(
            item =>
                item.ScheduleEntryId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
        var enrollmentCode = await BusinessCodeFormatter.DeriveAsync(db, entry.TimetableCode, "timetable", "enrollment", cancellationToken);
        if (enrollment is null)
        {
            enrollment = new TimetableEnrollment
            {
                ScheduleEntryId = id,
                AcademicYear = period.AcademicYear,
                Semester = period.Semester
            };
            db.TimetableEnrollments.Add(enrollment);
        }

        enrollment.EnrollmentCode = enrollmentCode;
        enrollment.CourseId = courseId;
        enrollment.Course = validated.Course;
        enrollment.TeacherId = teacherId;
        enrollment.Teacher = validated.Teacher;
        enrollment.ClassroomId = classroomId;
        enrollment.Classroom = validated.Classroom;
        enrollment.YearLevel = yearLevel;
        enrollment.Status = "Active";
        enrollment.UpdatedAtUtc = DateTime.UtcNow;

        // Keep legacy schedule columns synchronized until all operational readers use enrollment-owned relationships.
        entry.CourseId = courseId;
        entry.Course = validated.Course;
        entry.TeacherId = teacherId;
        entry.Teacher = validated.Teacher;
        entry.ClassroomId = classroomId;
        entry.Classroom = validated.Classroom;
        entry.YearLevel = yearLevel;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        var auditValues = AssignmentValues(
            ("timetableCode", entry.TimetableCode),
            ("enrollmentCode", enrollment.EnrollmentCode),
            ("courseId", courseId.ToString()),
            ("teacherId", teacherId.ToString()),
            ("classroomId", classroomId.ToString()),
            ("yearLevel", yearLevel.ToString()),
            ("shift", entry.Shift),
            ("academicYear", period.AcademicYear),
            ("semester", period.Semester),
            ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Timetable",
            entry.TimetableCode,
            "Enrollment updated",
            auditValues));

        return TimetableEnrollmentItemFactory.Create(
            entry,
            enrollment,
            validated.DepartmentId,
            validated.DepartmentName);
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var enrollment = await db.TimetableEnrollments
            .Include(item => item.ScheduleEntry)
            .FirstOrDefaultAsync(
                item =>
                    item.ScheduleEntryId == id
                    && item.AcademicYear == period.AcademicYear
                    && item.Semester == period.Semester,
                cancellationToken);
        if (enrollment is null || enrollment.Status == "Removed" || enrollment.ScheduleEntry is null)
            return false;

        var entry = enrollment.ScheduleEntry;
        var values = AssignmentValues(
            ("enrollmentCode", enrollment.EnrollmentCode),
            ("timetableCode", entry.TimetableCode),
            ("courseId", enrollment.CourseId.ToString()),
            ("teacherId", enrollment.TeacherId.ToString()),
            ("classroomId", enrollment.ClassroomId.ToString()),
            ("yearLevel", enrollment.YearLevel.ToString()),
            ("dayOfWeek", entry.DayOfWeek.ToString()),
            ("startsAt", entry.StartsAt.ToString("HH:mm")),
            ("endsAt", entry.EndsAt.ToString("HH:mm")),
            ("academicYear", enrollment.AcademicYear),
            ("semester", enrollment.Semester),
            ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));

        enrollment.Status = "Removed";
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Timetable",
            entry.TimetableCode,
            "Assignment removed",
            values));
        return true;
    }
}
