namespace InstituteManagement.Application.Features.Notifications.Announcements;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementItemDto>> GetAsync(CancellationToken cancellationToken);
    Task<AnnouncementItemDto> CreateAsync(AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> UpdateAsync(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
