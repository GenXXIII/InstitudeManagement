namespace InstituteManagement.Application.Features.Record;

public interface IOperationalRecordEditService
{
    Task UpdateClassSessionAsync(Guid id, UpdateClassSessionRecordDto update, CancellationToken cancellationToken);
}
