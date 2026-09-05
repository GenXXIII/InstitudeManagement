using MediatR;

namespace InstituteManagement.Application.Features.Management.Courses.GetCourses;

public sealed record GetCoursesQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<CourseResponseDto>>;
