using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.UpdateCourse;

public sealed class UpdateCourseHandler(ICourseManagementService service) : IRequestHandler<UpdateCourseCommand, CourseResponseDto>
{
    public Task<CourseResponseDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
