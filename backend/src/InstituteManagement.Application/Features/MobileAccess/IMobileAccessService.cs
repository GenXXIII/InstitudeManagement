namespace InstituteManagement.Application.Features.MobileAccess;

public interface IMobileAccessService
{
    Task<MobileSessionDto?> SignInAsync(string publicId, string password, CancellationToken cancellationToken);
}

public sealed record MobileSessionDto(string Role, string PublicId, Guid ProfileId);
