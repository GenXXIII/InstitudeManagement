using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.UpdateStudent;

public sealed record UpdateStudentCommand(Guid Id, Dictionary<string, string> Values) : IRequest<StudentResponseDto>;
