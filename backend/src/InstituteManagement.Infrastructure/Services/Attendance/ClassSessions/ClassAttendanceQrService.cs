using InstituteManagement.Application.Features.Attendance;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;

public sealed class ClassAttendanceQrService(
    InstituteDbContext db,
    ClassAttendanceQrGateway qrGateway,
    IAttendanceService attendanceService) : IClassAttendanceQrService
{
    public async Task<ClassAttendanceQrDto> GenerateAsync(
        Guid scheduleEntryId,
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        if (scheduleEntryId == Guid.Empty) throw new ArgumentException("Schedule entry is required.", nameof(scheduleEntryId));
        if (teacherId == Guid.Empty) throw new ArgumentException("Teacher is required.", nameof(teacherId));

        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var sessionDate = DateOnly.FromDateTime(localNow);
        var session = await db.ClassSessionStarts.AsNoTracking()
            .Include(item => item.ScheduleEntry)
            .SingleOrDefaultAsync(
                item => item.ScheduleEntryId == scheduleEntryId && item.SessionDate == sessionDate,
                cancellationToken)
            ?? throw new InvalidOperationException("Start this class before opening its attendance QR.");

        if (session.TeacherId != teacherId)
            throw new InvalidOperationException("Only the assigned Teacher can open this class attendance QR.");
        EnsureRunning(session.ScheduleEntry, localNow);
        return qrGateway.Generate(session);
    }

    public async Task<ClassAttendanceCheckInDto> CheckInAsync(
        Guid scheduleEntryId,
        Guid studentId,
        string qrPayload,
        CancellationToken cancellationToken)
    {
        if (scheduleEntryId == Guid.Empty) throw new ArgumentException("Schedule entry is required.", nameof(scheduleEntryId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));

        var claims = qrGateway.Validate(qrPayload);
        if (claims.ScheduleEntryId != scheduleEntryId)
            throw new ArgumentException("This QR belongs to a different class.");

        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var sessionDate = DateOnly.FromDateTime(localNow);
        var session = await db.ClassSessionStarts.AsNoTracking()
            .Include(item => item.ScheduleEntry)!
                .ThenInclude(item => item!.Course)
            .SingleOrDefaultAsync(
                item => item.Id == claims.ClassSessionStartId
                    && item.ScheduleEntryId == scheduleEntryId
                    && item.TeacherId == claims.TeacherId
                    && item.SessionDate == sessionDate,
                cancellationToken)
            ?? throw new InvalidOperationException("This class session is no longer available.");
        EnsureRunning(session.ScheduleEntry, localNow);

        var enrollment = await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.StudentId == studentId && item.Status == "Active")
            .OrderByDescending(item => item.CreateAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The Student does not have an active enrollment.");
        var schedule = session.ScheduleEntry!;
        var belongsToClass = schedule.Course?.DepartmentId == enrollment.DepartmentId
            && schedule.YearLevel == enrollment.YearLevel
            && schedule.Shift == enrollment.Shift;
        if (!belongsToClass)
            throw new InvalidOperationException("This attendance QR is for a different Student class.");

        var timetableIsActive = await db.TimetableEnrollments.AsNoTracking()
            .AnyAsync(item => item.ScheduleEntryId == scheduleEntryId && item.Status == "Active", cancellationToken);
        if (!timetableIsActive)
            throw new InvalidOperationException("This timetable enrollment is not active.");

        await attendanceService.RecordAsync(studentId, "Present", cancellationToken, "Dynamic QR");
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
            throw new InvalidOperationException("The attendance QR is available only while this class is running.");
    }
}
