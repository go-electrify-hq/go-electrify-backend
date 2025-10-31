using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface INotificationRepository
    {
        //Lấy danh sách thông báo phù hợp với vai trò người dùng (Driver / Staff / Admin).
        Task<List<NotificationDto>> GetByRoleAsync(int userId, string role, CancellationToken ct);

        //Lấy danh sách ID tất cả thông báo của user (dùng khi đánh dấu "đã đọc tất cả").
        Task<List<string>> GetAllIdsAsync(int userId, CancellationToken ct);
        Task<bool> IsVisibleIdAsync(int userId, string role, string notifId, CancellationToken ct); // NEW
    }
}
