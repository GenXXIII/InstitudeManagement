using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.CreateCourse;

public sealed class CreateCourseHandler(ICourseManagementService service) : IRequestHandler<CreateCourseCommand, CourseResponseDto>
{
    public Task<CourseResponseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
