using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;

namespace GoElectrify.BLL.Services
{
    /// <summary>
    /// Service hợp nhất dữ liệu dashboard + tính IsNew/IsUnread + thao tác seen/read.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        public NotificationService(INotificationRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(int userId, string role, CancellationToken ct)
        {
            // Chạy song song để giảm latency tổng
            var items = await _repo.GetByRoleAsync(userId, role, ct);
            var lastSeen = await _repo.GetLastSeenUtcAsync(userId, ct) ?? DateTime.MinValue;
            var readSet = await _repo.GetReadKeysAsync(userId, ct);

            if (lastSeen.Kind != DateTimeKind.Utc)
                lastSeen = DateTime.SpecifyKind(lastSeen, DateTimeKind.Utc);

            foreach (var n in items)
            {
                var createdUtc = n.CreatedAt.Kind == DateTimeKind.Utc
                    ? n.CreatedAt
                    : n.CreatedAt.ToUniversalTime();

                n.IsNew = createdUtc > lastSeen;     // badge đỏ
                n.IsUnread = !readSet.Contains(n.Id);   // chấm xanh
            }

            return items.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public Task MarkSeenAsync(int userId, CancellationToken ct)
            => _repo.UpsertLastSeenUtcAsync(userId, DateTime.UtcNow, ct);

        public async Task MarkAllReadAsync(int userId, string role, CancellationToken ct)
        {
            var keys = await _repo.GetAllIdsAsync(userId, role, ct).ConfigureAwait(false);
            if (keys.Count == 0) return;
            await _repo.MarkAllReadAsync(userId, keys, ct).ConfigureAwait(false);
        }

        public async Task<bool> MarkOneReadAsync(int userId, string id, string role, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            var visible = await _repo.IsVisibleIdAsync(userId, role, id, ct).ConfigureAwait(false);
            if (!visible) return false;

            await _repo.MarkReadAsync(userId, id, ct).ConfigureAwait(false);
            return true;
        }
    }
}
