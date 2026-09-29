using InstituteManagement.Application.Features.Attendance;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Domain.Common;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;

public sealed class ClassAttendanceQrService(
    InstituteDbContext db,
    IAttendanceService attendanceService) : IClassAttendanceQrService
{
    public async Task<ClassAttendanceCheckInDto> CheckInAsync(
        Guid scheduleEntryId,
        Guid studentId,
        string qrPayload,
        CancellationToken cancellationToken)
    {
        if (scheduleEntryId == Guid.Empty) throw new ArgumentException("Schedule entry is required.", nameof(scheduleEntryId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));

        var scannedPublicId = AttendanceIdentityQr.ReadStudent(qrPayload);
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var sessionDate = DateOnly.FromDateTime(localNow);
        var session = await db.ClassSessionStarts.AsNoTracking()
            .Include(item => item.ScheduleEntry)!
                .ThenInclude(item => item!.Course)
            .SingleOrDefaultAsync(
                item => item.ScheduleEntryId == scheduleEntryId
                    && item.SessionDate == sessionDate,
                cancellationToken)
            ?? throw new InvalidOperationException("The Teacher must start this class before Student attendance can be scanned.");
        EnsureRunning(session.ScheduleEntry, localNow);

        var schedule = session.ScheduleEntry!;
        var enrollment = await EnsureStudentBelongsAsync(studentId, schedule, cancellationToken);
        if (!enrollment.PublicId.Equals(scannedPublicId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Scan the attendance QR assigned to your signed-in Student Public ID.");

        await EnsureTimetableIsActiveAsync(scheduleEntryId, cancellationToken);
        return await RecordAsync(scheduleEntryId, studentId, sessionDate, cancellationToken);
    }

    private async Task<StudentEnrollment> EnsureStudentBelongsAsync(
        Guid studentId,
        ScheduleEntry schedule,
        CancellationToken cancellationToken)
    {
        var enrollment = await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.StudentId == studentId && item.Status == "Active")
            .OrderByDescending(item => item.CreateAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The Student does not have an active enrollment.");
        var belongsToClass = schedule.YearLevel == enrollment.YearLevel
            && StudentCurriculumPolicy.IncludesDepartment(enrollment.YearLevel, enrollment.DepartmentId, schedule.Course?.DepartmentId)
            && schedule.Shift == enrollment.Shift;
        if (!belongsToClass)
            throw new InvalidOperationException("This attendance QR is for a different Student class.");
        return enrollment;
    }

    private async Task EnsureTimetableIsActiveAsync(Guid scheduleEntryId, CancellationToken cancellationToken)
    {
        var timetableIsActive = await db.TimetableEnrollments.AsNoTracking()
            .AnyAsync(item => item.ScheduleEntryId == scheduleEntryId && item.Status == "Active", cancellationToken);
        if (!timetableIsActive)
            throw new InvalidOperationException("This timetable enrollment is not active.");
    }

    private async Task<ClassAttendanceCheckInDto> RecordAsync(
        Guid scheduleEntryId,
        Guid studentId,
        DateOnly sessionDate,
        CancellationToken cancellationToken)
    {
        await attendanceService.RecordAsync(studentId, "Present", cancellationToken, "Public ID QR", applyLateRule: false);
        var record = await db.AttendanceRecords.AsNoTracking()
            .SingleAsync(item => item.StudentId == studentId && item.Date == sessionDate, cancellationToken);
        return new ClassAttendanceCheckInDto(
            scheduleEntryId,
            studentId,
            record.Status,
            record.Date,
            record.CheckedInAt,
            record.Method);
    }

    private static void EnsureRunning(ScheduleEntry? schedule, DateTime localNow)
    {
        if (schedule is null || schedule.Status == "Cancelled")
            throw new InvalidOperationException("This timetable class is unavailable.");
        var localTime = TimeOnly.FromDateTime(localNow);
        if (schedule.DayOfWeek != localNow.DayOfWeek || localTime < schedule.StartsAt || localTime >= schedule.EndsAt)
            throw new InvalidOperationException("Student attendance is available only while this class is running.");
    }
}
