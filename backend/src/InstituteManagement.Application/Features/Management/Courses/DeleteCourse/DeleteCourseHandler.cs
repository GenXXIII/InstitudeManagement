using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.DeleteCourse;

public sealed class DeleteCourseHandler(ICourseManagementService service) : IRequestHandler<DeleteCourseCommand, bool>
{
    public Task<bool> Handle(DeleteCourseCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
