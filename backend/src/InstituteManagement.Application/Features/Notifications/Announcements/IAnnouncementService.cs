using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Notifications.Announcements;

public interface IAnnouncementService
{
    Task<PagedResult<AnnouncementItemDto>> GetAsync(string? search, bool unreadOnly, bool prioritizeUnread, PageRequest page, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> CreateAsync(AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> MarkReadAsync(Guid id, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> UpdateAsync(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
