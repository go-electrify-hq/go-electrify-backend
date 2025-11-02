using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GoElectrify.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
    private readonly IDistributedCache _cache;

    public NotificationService(INotificationRepository repo, IDistributedCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

        private static string LastSeenKey(int userId) => $"notif:lastseen:{userId}";
        private static string ReadSetKey(int userId) => $"notif:readset:{userId}";

        // Lấy danh sách + gắn trạng thái IsNew/IsUnread
        private static DateTime ParseUtcOrMin(string? iso8601)
        {
            if (string.IsNullOrWhiteSpace(iso8601)) return DateTime.MinValue;
            if (DateTime.TryParseExact(iso8601, "O", null, DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToUniversalTime();
            return DateTime.MinValue;
        }

        private static DateTime AsUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt.ToUniversalTime();
        }

        // GET /dashboard
        public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(int userId, string role, CancellationToken ct)
        {
            var list = await _repo.GetByRoleAsync(userId, role, ct);

            // đọc cache
            var lastSeenStr = await _cache.GetStringAsync(LastSeenKey(userId), ct);
            var lastSeen = ParseUtcOrMin(lastSeenStr);

            var readStr = await _cache.GetStringAsync(ReadSetKey(userId), ct);
            var readSet = string.IsNullOrEmpty(readStr)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(readStr) ?? new();

            // gắn trạng thái
            foreach (var n in list)
            {
                var created = AsUtc(n.CreatedAt);
                n.IsNew = created > lastSeen;          // badge đỏ
                n.IsUnread = !readSet.Contains(n.Id);  // chấm xanh
            }

            return list.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public async Task MarkSeenAsync(int userId, CancellationToken ct)
        => await _cache.SetStringAsync(LastSeenKey(userId), DateTime.UtcNow.ToString("O"), ct);

        public async Task MarkAllReadAsync(int userId, string role, CancellationToken ct)
        {
            var ids = await _repo.GetAllIdsAsync(userId, role, ct);
            var json = JsonSerializer.Serialize(ids.ToHashSet());
            await _cache.SetStringAsync(ReadSetKey(userId), json, ct);
        }

        public async Task<bool> MarkOneReadAsync(int userId, string id, string role, CancellationToken ct)
        {
            var visible = await _repo.IsVisibleIdAsync(userId, role, id, ct);
            if (!visible) return false;

            var readStr = await _cache.GetStringAsync(ReadSetKey(userId), ct);
            var set = string.IsNullOrEmpty(readStr)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(readStr) ?? new();

            set.Add(id);
            await _cache.SetStringAsync(ReadSetKey(userId), JsonSerializer.Serialize(set), ct);
            return true;
        }
    }
}
