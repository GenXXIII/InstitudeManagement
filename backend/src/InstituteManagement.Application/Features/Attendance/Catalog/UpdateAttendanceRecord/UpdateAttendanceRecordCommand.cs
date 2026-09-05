using MediatR;

namespace InstituteManagement.Application.Features.Attendance.UpdateAttendanceRecord;

public sealed record UpdateAttendanceRecordCommand(Guid Id, Dictionary<string, string> Values) : IRequest<AttendanceResponseDto>;
