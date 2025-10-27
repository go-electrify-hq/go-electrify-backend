using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

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
        private static string ReadSetKey(int userId) => $"notif:read:{userId}"; // << bổ sung

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(
            NotificationQueryDto query, int userId, string role, CancellationToken ct)
        {
            var list = await _repository.GetDashboardBaseAsync(query, userId, role, ct);

            // 1) lastSeen
            DateTime? lastSeen = await GetLastSeenAsync(userId, ct);

            // 2) readSet (các id đã đọc lẻ)
            var readSet = await GetReadSetAsync(userId, ct);

            // 3) Hợp nhất: IsNew = !(đã cũ hơn/ bằng lastSeen hoặc nằm trong readSet)
            foreach (var n in list)
            {
                bool readByLastSeen = lastSeen.HasValue && n.CreatedAt <= lastSeen.Value;
                bool readIndividually = readSet.Contains(n.Id);
                n.IsNew = !(readByLastSeen || readIndividually);
            }

            return list;
        }

        public async Task MarkAllReadNowAsync(int userId, CancellationToken ct)
        {
            var key = LastSeenKey(userId);
            var nowIso = DateTime.UtcNow.ToString("O");
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(nowIso),
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(30) }, ct);

            // tuỳ chọn: clear readSet vì lastSeen mới đã bao hết các item cũ
            await _cache.RemoveAsync(ReadSetKey(userId), ct);
        }

        // NEW: đánh dấu đã đọc một thông báo
        public async Task MarkOneReadAsync(int userId, string notificationId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(notificationId)) return;

            var key = ReadSetKey(userId);
            var bytes = await _cache.GetAsync(key, ct);

            HashSet<string> set = new(StringComparer.Ordinal);
            if (bytes is { Length: > 0 })
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<ReadSetPayload>(bytes);
                    if (payload?.Ids is not null) set = payload.Ids.ToHashSet(StringComparer.Ordinal);
                }
                catch { set = new HashSet<string>(StringComparer.Ordinal); }
            }

            set.Add(notificationId);

            // giới hạn kích thước cache
            if (set.Count > 500) set = set.Take(500).ToHashSet(StringComparer.Ordinal);

            var save = new ReadSetPayload { Ids = set.ToList() };
            var json = JsonSerializer.SerializeToUtf8Bytes(save);

            await _cache.SetAsync(key, json, new DistributedCacheEntryOptions
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

        private sealed record ReadSetPayload { public List<string>? Ids { get; init; } }

        private async Task<HashSet<string>> GetReadSetAsync(int userId, CancellationToken ct)
        {
            var key = ReadSetKey(userId);
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            try
            {
                var payload = JsonSerializer.Deserialize<ReadSetPayload>(bytes);
                return payload?.Ids?.ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
            }
            catch
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }
    }
}
