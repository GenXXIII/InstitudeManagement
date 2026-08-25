namespace InstituteManagement.Application.DTOs.Enrollment;

public sealed record EnrollmentItemDto(Guid Id, IReadOnlyDictionary<string, string> Values);
