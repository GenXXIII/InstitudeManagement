namespace InstituteManagement.Application.Features.Enrollment;

public sealed record EnrollmentItemDto(Guid Id, IReadOnlyDictionary<string, string> Values);
