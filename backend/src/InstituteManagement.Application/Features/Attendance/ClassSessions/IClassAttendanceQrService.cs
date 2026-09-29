namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public interface IClassAttendanceQrService
{
    Task<ClassAttendanceCheckInDto> CheckInAsync(
        Guid scheduleEntryId,
        Guid studentId,
        string qrPayload,
        CancellationToken cancellationToken);
}
