using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableEnrollmentEditor(
    InstituteDbContext db,
    TimetableScheduleEditor scheduleEditor,
    TimetableEnrollmentValidator validator)
{
    public async Task<EnrollmentItemDto> UpdateAsync(
        Guid id,
        Dictionary<string, string> values,
        EnrollmentPeriod period,
        CancellationToken cancellationToken)
    {
        var entry = await db.ScheduleEntries
            .Include(item => item.Course)
            .Include(item => item.Teacher)
            .Include(item => item.Classroom)
            .FirstOrDefaultAsync(
                item => item.Id == id && item.Status != "Cancelled",
                cancellationToken)
            ?? throw new KeyNotFoundException("Management schedule not found.");

        if (values.ContainsKey("timetableCode"))
        {
            await scheduleEditor.ApplyAsync(entry, values, cancellationToken);
        }

        var course = await validator.ValidateAsync(entry, period, cancellationToken);
        var enrollment = await db.TimetableEnrollments.FirstOrDefaultAsync(
            item =>
                item.ScheduleEntryId == id
                && item.AcademicYear == period.AcademicYear
                && item.Semester == period.Semester,
            cancellationToken);
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

        enrollment.Status = "Active";
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(EnrollmentAuditFactory.Create(
            id,
            "Timetable",
            entry.TimetableCode,
            "Enrollment added",
            AssignmentValues(
                ("timetableCode", entry.TimetableCode),
                ("classroomStatus", entry.Classroom?.Status ?? "Maintenance"),
                ("academicYear", period.AcademicYear),
                ("semester", period.Semester))));

        return TimetableEnrollmentItemFactory.Create(
            entry,
            course.DepartmentId,
            course.Department?.Name,
            enrollment.Status);
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

        if (enrollment is null
            || enrollment.Status == "Removed"
            || enrollment.ScheduleEntry is null)
        {
            return false;
        }

        var entry = enrollment.ScheduleEntry;
        var values = AssignmentValues(
            ("timetableCode", entry.TimetableCode),
            ("courseId", entry.CourseId.ToString()),
            ("teacherId", entry.TeacherId.ToString()),
            ("classroomId", entry.ClassroomId.ToString()),
            ("yearLevel", entry.YearLevel.ToString()),
            ("dayOfWeek", entry.DayOfWeek.ToString()),
            ("startsAt", entry.StartsAt.ToString("HH:mm")),
            ("endsAt", entry.EndsAt.ToString("HH:mm")),
            ("academicYear", enrollment.AcademicYear),
            ("semester", enrollment.Semester));

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
