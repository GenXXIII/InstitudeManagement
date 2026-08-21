namespace InstituteManagement.Application.DTOs;

public sealed record OperationalRecordDto(Guid Id, string Module, string Subject, string Identifier, string Status, string Summary, DateTime? LastActivityAt, IReadOnlyList<Dictionary<string, string>> Activities);
