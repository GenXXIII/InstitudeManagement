namespace InstituteManagement.Application.Features.Record;

public sealed record RecordDto(Guid Id, Guid? ResourceId, DateTime Date, string Type, string Subject, string Action, string Details, string AuditLogCode = "");
