using MediatR;

namespace InstituteManagement.Application.Features.Attendance.CreateAttendanceRecord;

public sealed record CreateAttendanceRecordCommand(Dictionary<string, string> Values) : IRequest<AttendanceResponseDto>;
