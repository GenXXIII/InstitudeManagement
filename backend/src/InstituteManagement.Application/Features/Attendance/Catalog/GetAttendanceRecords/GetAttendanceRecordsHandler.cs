using MediatR;

namespace InstituteManagement.Application.Features.Attendance.GetAttendanceRecords;

public sealed class GetAttendanceRecordsHandler(IAttendanceCatalogService service) : IRequestHandler<GetAttendanceRecordsQuery, IReadOnlyList<AttendanceResponseDto>>
{
    public Task<IReadOnlyList<AttendanceResponseDto>> Handle(GetAttendanceRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
