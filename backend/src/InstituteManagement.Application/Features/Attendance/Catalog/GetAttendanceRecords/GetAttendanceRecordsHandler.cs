using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Attendance.GetAttendanceRecords;

public sealed class GetAttendanceRecordsHandler(IAttendanceCatalogService service) : IRequestHandler<GetAttendanceRecordsQuery, PagedResult<AttendanceResponseDto>>
{
    public Task<PagedResult<AttendanceResponseDto>> Handle(GetAttendanceRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.Shift, request.Page, cancellationToken);
}
