using InstituteManagement.Application.Common.Pagination;
using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.GetTimetableEnrollments;

public sealed class GetTimetableEnrollmentsHandler(ITimetableEnrollmentService service)
    : IRequestHandler<GetTimetableEnrollmentsQuery, PagedResult<EnrollmentItemDto>>
{
    public Task<PagedResult<EnrollmentItemDto>> Handle(GetTimetableEnrollmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.Page, cancellationToken);
}
