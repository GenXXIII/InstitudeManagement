using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IOperationalRecordEditService
{
    Task UpdateClassSessionAsync(Guid id, UpdateClassSessionRecordDto update, CancellationToken cancellationToken);
}
