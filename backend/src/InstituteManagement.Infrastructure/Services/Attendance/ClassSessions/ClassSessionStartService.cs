using InstituteManagement.Application.Common.LiveUpdates;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Policies;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;

public sealed class ClassSessionStartService(
    InstituteDbContext db,
    InstituteCache cache,
    ILiveUpdatePublisher publisher) : IClassSessionStartService
{
    public async Task<ClassSessionStartDto> StartAsync(
        Guid scheduleEntryId,
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        if (scheduleEntryId == Guid.Empty) throw new ArgumentException("Schedule entry is required.", nameof(scheduleEntryId));
        if (teacherId == Guid.Empty) throw new ArgumentException("Teacher is required.", nameof(teacherId));

        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var sessionDate = DateOnly.FromDateTime(localNow);
        var existing = await db.ClassSessionStarts.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.ScheduleEntryId == scheduleEntryId && item.SessionDate == sessionDate,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.TeacherId != teacherId)
                throw new InvalidOperationException("This class was started by its assigned teacher.");
            return Map(existing);
        }

        var schedule = await db.ScheduleEntries.AsNoTracking()
            .Include(item => item.Teacher)
            .Include(item => item.Classroom)
            .FirstOrDefaultAsync(item => item.Id == scheduleEntryId, cancellationToken)
            ?? throw new KeyNotFoundException("Timetable class not found.");

        if (schedule.TeacherId != teacherId)
            throw new InvalidOperationException("Only the teacher assigned to this timetable can start the class.");
        if (schedule.Status == "Cancelled")
            throw new InvalidOperationException("A cancelled timetable class cannot be started.");
        if (schedule.Teacher is null || !TeacherPresence.IsPresent(TeacherPresence.Attendance(schedule.Teacher.Status)))
            throw new InvalidOperationException("The assigned teacher is unavailable and cannot start this class.");
        if (schedule.Classroom is null || schedule.Classroom.Status != "Available" || !schedule.Classroom.DeviceOnline)
            throw new InvalidOperationException("The assigned classroom is unavailable and this class cannot start.");

        var hasActiveEnrollment = await db.TimetableEnrollments.AsNoTracking()
            .AnyAsync(item => item.ScheduleEntryId == scheduleEntryId && item.Status == "Active", cancellationToken);
        if (!hasActiveEnrollment)
            throw new InvalidOperationException("This timetable does not have an active enrollment.");

        var localTime = TimeOnly.FromDateTime(localNow);
        if (schedule.DayOfWeek != localNow.DayOfWeek || localTime < schedule.StartsAt || localTime >= schedule.EndsAt)
            throw new InvalidOperationException($"This class can start only during its timetable period ({schedule.DayOfWeek}, {schedule.StartsAt:HH:mm}-{schedule.EndsAt:HH:mm}).");

        var entity = new ClassSessionStart
        {
            ScheduleEntryId = schedule.Id,
            TeacherId = teacherId,
            SessionDate = sessionDate,
            StartedAtUtc = DateTime.UtcNow
        };
        db.ClassSessionStarts.Add(entity);
        db.AuditLogs.Add(new AuditLog
        {
            ResourceId = entity.Id,
            Type = "Class session",
            Subject = schedule.TimetableCode,
            Action = "Class started",
            Details = $"{sessionDate:yyyy-MM-dd} {schedule.StartsAt:HH:mm}-{schedule.EndsAt:HH:mm} · {schedule.Teacher.FullName}"
        });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        await publisher.PublishAsync(
            "CLASS_STARTED",
            new { entity.Id, entity.ScheduleEntryId, entity.TeacherId, entity.SessionDate, entity.StartedAtUtc },
            cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<ClassSessionStartDto>> GetTodayAsync(
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty) throw new ArgumentException("Teacher is required.", nameof(teacherId));
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var sessionDate = DateOnly.FromDateTime(localNow);
        return await db.ClassSessionStarts.AsNoTracking()
            .Where(item => item.TeacherId == teacherId && item.SessionDate == sessionDate)
            .OrderBy(item => item.StartedAtUtc)
            .Select(item => new ClassSessionStartDto(
                item.Id,
                item.ScheduleEntryId,
                item.TeacherId,
                item.SessionDate,
                item.StartedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static ClassSessionStartDto Map(ClassSessionStart item) =>
        new(item.Id, item.ScheduleEntryId, item.TeacherId, item.SessionDate, item.StartedAtUtc);
}
