using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.GetCourses;

public sealed class GetCoursesHandler(ICourseManagementService service) : IRequestHandler<GetCoursesQuery, IReadOnlyList<CourseResponseDto>>
{
    public Task<IReadOnlyList<CourseResponseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
