using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.CreateStudent;

public sealed record CreateStudentCommand(Dictionary<string, string> Values) : IRequest<StudentResponseDto>;
