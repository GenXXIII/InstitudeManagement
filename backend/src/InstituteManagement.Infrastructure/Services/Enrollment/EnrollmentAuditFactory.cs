using System.Text.Json;
using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal static class EnrollmentAuditFactory
{
    public static AuditLog Create(
        Guid id,
        string type,
        string subject,
        string action,
        IReadOnlyDictionary<string, string> values) =>
        new()
        {
            ResourceId = id,
            Type = type,
            Subject = subject,
            Action = action,
            Details = JsonSerializer.Serialize(values)
        };
}
