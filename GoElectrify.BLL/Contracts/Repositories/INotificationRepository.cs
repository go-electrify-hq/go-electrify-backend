using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface INotificationRepository
    {
        // ===== Sự kiện (render dashboard) =====
        Task<List<NotificationDto>> GetByRoleAsync(int userId, string role, CancellationToken ct);
        Task<List<string>> GetAllIdsAsync(int userId, string role, CancellationToken ct);
        Task<bool> IsVisibleIdAsync(int userId, string role, string notifId, CancellationToken ct);

        // ===== Trạng thái (table Notifications) =====
        Task<DateTime?> GetLastSeenUtcAsync(int userId, CancellationToken ct);
        Task UpsertLastSeenUtcAsync(int userId, DateTime lastSeenUtc, CancellationToken ct);

        Task<HashSet<string>> GetReadKeysAsync(int userId, CancellationToken ct);
        Task MarkReadAsync(int userId, string notifKey, CancellationToken ct);
        Task MarkAllReadAsync(int userId, IEnumerable<string> notifKeys, CancellationToken ct);
    }
}
