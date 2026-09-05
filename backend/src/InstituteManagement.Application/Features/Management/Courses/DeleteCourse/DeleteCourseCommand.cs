using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(Guid Id) : IRequest<bool>;
