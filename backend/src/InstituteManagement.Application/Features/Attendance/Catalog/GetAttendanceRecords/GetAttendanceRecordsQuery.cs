using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Attendance.GetAttendanceRecords;

public sealed record GetAttendanceRecordsQuery(string? Search, Guid? DepartmentId, int? Year, string? Shift, PageRequest Page) : IRequest<PagedResult<AttendanceResponseDto>>;
