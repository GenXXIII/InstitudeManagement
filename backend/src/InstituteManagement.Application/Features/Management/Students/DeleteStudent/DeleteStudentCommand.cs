using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.DeleteStudent;

public sealed record DeleteStudentCommand(Guid Id) : IRequest<bool>;
