using InstituteManagement.Application.Common.Startup;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassSessionRecorderStartupTask(ClassSessionRecorderService recorder) : IApplicationStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken) =>
        await recorder.RecordCompletedForCurrentTimeAsync(cancellationToken);
}
