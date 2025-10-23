using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace GoElectrify.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IDistributedCache _cache;

        public NotificationService(INotificationRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        private static string LastSeenKey(int userId) => $"notif:lastseen:{userId}";

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(
            NotificationQueryDto query, int userId, string role, CancellationToken ct)
        {
            var list = await _repository.GetDashboardBaseAsync(query, userId, role, ct);

            // lấy lastSeen từ cache
            DateTime? lastSeen = await GetLastSeenAsync(userId, ct);

            if (lastSeen.HasValue)
            {
                var t = lastSeen.Value;
                foreach (var n in list)
                    n.IsNew = n.CreatedAt > t;
            }
            else
            {
                // Nếu chưa có lastSeen → coi tất cả là mới
                foreach (var n in list) n.IsNew = true;
            }

            return list;
        }

        public async Task MarkAllReadNowAsync(int userId, CancellationToken ct)
        {
            var key = LastSeenKey(userId);
            var nowIso = DateTime.UtcNow.ToString("O");
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(nowIso), new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromDays(30)
            }, ct);
        }

        private async Task<DateTime?> GetLastSeenAsync(int userId, CancellationToken ct)
        {
            var key = LastSeenKey(userId);
            var data = await _cache.GetAsync(key, ct);
            if (data is null || data.Length == 0) return null;
            var iso = Encoding.UTF8.GetString(data);
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                return dt.ToUniversalTime();
            return null;
        }
    }
}
