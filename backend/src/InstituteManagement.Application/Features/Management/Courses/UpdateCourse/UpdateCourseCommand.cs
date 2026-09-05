using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.UpdateCourse;

public sealed record UpdateCourseCommand(Guid Id, Dictionary<string, string> Values) : IRequest<CourseResponseDto>;
