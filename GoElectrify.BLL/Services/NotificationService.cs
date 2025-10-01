using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;

namespace GoElectrify.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        public NotificationService(INotificationRepository repo) => _repo = repo;

        public Task<IReadOnlyList<NotificationDto>> GetDashboardNotificationsAsync()
            => _repo.GetDashboardNotificationsAsync();
    }
}
