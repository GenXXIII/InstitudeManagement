using MediatR;

namespace InstituteManagement.Application.Features.Attendance.GetAttendanceRecords;

public sealed record GetAttendanceRecordsQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<AttendanceResponseDto>>;
