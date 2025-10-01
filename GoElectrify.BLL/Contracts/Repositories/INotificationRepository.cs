using GoElectrify.BLL.Dto.Notification;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<NotificationDto>> GetDashboardNotificationsAsync();
    }
}
