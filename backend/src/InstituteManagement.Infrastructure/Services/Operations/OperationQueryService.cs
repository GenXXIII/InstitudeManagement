using InstituteManagement.Application.Features.Operations;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class OperationQueryService(IEnumerable<IOperationModuleReader> readers) : IOperationQueryService
{
    private readonly IReadOnlyDictionary<string, IOperationModuleReader> _readers = readers.ToDictionary(x => x.Module, StringComparer.OrdinalIgnoreCase);

    public Task<OperationDto> GetAsync(string module, Guid? departmentId, CancellationToken cancellationToken)
    {
        var key = module.Equals("control-room", StringComparison.OrdinalIgnoreCase) ? "dashboard" : module;
        return _readers.TryGetValue(key, out var reader)
            ? reader.GetAsync(departmentId, cancellationToken)
            : throw new ArgumentException($"Operation module '{module}' is not supported.");
    }
}
