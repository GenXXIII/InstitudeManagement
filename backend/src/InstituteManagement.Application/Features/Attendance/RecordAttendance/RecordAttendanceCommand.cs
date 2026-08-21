using MediatR;

namespace InstituteManagement.Application.Features.Attendance.RecordAttendance;

public sealed record RecordAttendanceCommand(Guid StudentId, string Status) : IRequest;
