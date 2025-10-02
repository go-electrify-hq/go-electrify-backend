using GoElectrify.BLL.Dto.Notification;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetDashboardNotificationsAsync();
    }
}
