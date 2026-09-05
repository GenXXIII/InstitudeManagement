using System.Text.Json;
using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.Infrastructure.Services.History;

public static class HistorySnapshotFactory
{
    public static RecordDto Create(Guid resourceId, DateTime date, string type, string subject, string action, object details) =>
        new(resourceId, resourceId, date, type, subject, action, JsonSerializer.Serialize(details));
}
