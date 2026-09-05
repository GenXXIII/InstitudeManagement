using MediatR;

namespace InstituteManagement.Application.Features.Attendance.DeleteAttendanceRecord;

public sealed record DeleteAttendanceRecordCommand(Guid Id) : IRequest<bool>;
