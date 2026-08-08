using SajhaSikshya.Data.Entities.Notifications;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Notifications;
using SajhaSikshya.Mappings.Notifications;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;

namespace SajhaSikshya.Services.Notifications;

public class NotificationQueryService : INotificationQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<NotificationDto>> GetHistoryAsync(string userId, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Notification>();
        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: n => n.UserId == userId,
            orderBy: q => q.OrderByDescending(n => n.CreatedAtUtc));

        return new PagedResult<NotificationDto>
        {
            Items = page.Items.Select(n => n.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId) =>
        await _unitOfWork.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(string userId)
    {
        var preference = await _unitOfWork.Repository<NotificationPreference>().FirstOrDefaultAsync(p => p.UserId == userId);
        return preference?.ToDto() ?? new NotificationPreferenceDto();
    }
}
