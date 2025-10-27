using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface INotificationService
    {
        // Lấy danh sách thông báo (có tính IsNew)
        Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(
            NotificationQueryDto query, int userId, string role, CancellationToken cancellationToken);

        // Đánh dấu tất cả thông báo là đã đọc
        Task MarkAllReadNowAsync(int userId, CancellationToken ct);
        Task MarkOneReadAsync(int userId, string notificationId, CancellationToken ct);
    }
}
