using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface INotificationService
    {
        //Lấy danh sách thông báo cho user (theo vai trò).
        Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(
            int userId, string role, CancellationToken ct);

        //Khi user mở popup thông báo → cập nhật thời điểm xem cuối.
        Task MarkSeenAsync(int userId, CancellationToken ct);

        //Đánh dấu tất cả thông báo là "đã đọc".
        Task MarkAllReadAsync(int userId, CancellationToken ct);

        //Đánh dấu một thông báo cụ thể là "đã đọc".
        //Task MarkOneReadAsync(int userId, string id, CancellationToken ct);
        Task<bool> MarkOneReadAsync(int userId, string id, string role, CancellationToken ct);
    }
}
