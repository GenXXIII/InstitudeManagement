namespace InstituteManagement.Application.Common.Startup;

public interface IApplicationStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
