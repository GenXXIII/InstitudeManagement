using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.CreateCourse;

public sealed record CreateCourseCommand(Dictionary<string, string> Values) : IRequest<CourseResponseDto>;
