using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<NotificationDto>> GetDashboardBaseAsync(
           NotificationQueryDto query, int userId, string role, CancellationToken cancellationToken);
    }
}
