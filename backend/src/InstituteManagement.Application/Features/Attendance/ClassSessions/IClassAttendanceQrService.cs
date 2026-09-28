namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public interface IClassAttendanceQrService
{
    Task<ClassAttendanceQrDto> GenerateAsync(
        Guid scheduleEntryId,
        Guid teacherId,
        CancellationToken cancellationToken);

    Task<ClassAttendanceQrDto> GenerateStudentAsync(
        Guid scheduleEntryId,
        Guid studentId,
        CancellationToken cancellationToken);

    Task<ClassAttendanceCheckInDto> CheckInAsync(
        Guid scheduleEntryId,
        Guid studentId,
        string qrPayload,
        CancellationToken cancellationToken);

    Task<ClassAttendanceCheckInDto> TeacherCheckInAsync(
        Guid scheduleEntryId,
        Guid teacherId,
        string qrPayload,
        CancellationToken cancellationToken);

}
