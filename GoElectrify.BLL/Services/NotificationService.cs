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
        private readonly INotificationRepository _repo;
    private readonly IDistributedCache _cache;

    public NotificationService(INotificationRepository repo, IDistributedCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    private static string LastSeenKey(int uid) => $"notif:lastseen:{uid}";
    private static string ReadSetKey(int uid) => $"notif:readset:{uid}";

    // Lấy danh sách + gắn trạng thái IsNew/IsUnread
    public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(int userId, string role, CancellationToken ct)
    {
        var list = await _repo.GetByRoleAsync(userId, role, ct);

        // Lấy cache
        var lastSeenStr = await _cache.GetStringAsync(LastSeenKey(userId), ct);
        var lastSeen = string.IsNullOrEmpty(lastSeenStr) ? DateTime.MinValue : DateTime.Parse(lastSeenStr);

        var readStr = await _cache.GetStringAsync(ReadSetKey(userId), ct);
        var readSet = string.IsNullOrEmpty(readStr)
            ? new HashSet<string>()
            : JsonSerializer.Deserialize<HashSet<string>>(readStr) ?? new();

        // Gắn trạng thái
        foreach (var n in list)
        {
            n.IsNew = n.CreatedAt > lastSeen;
            n.IsUnread = !readSet.Contains(n.Id);
        }

        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task MarkSeenAsync(int userId, CancellationToken ct)
        => await _cache.SetStringAsync(LastSeenKey(userId), DateTime.UtcNow.ToString("O"), ct);

    public async Task MarkAllReadAsync(int userId, CancellationToken ct)
    {
        var ids = await _repo.GetAllIdsAsync(userId, ct);
        await _cache.SetStringAsync(ReadSetKey(userId),
            JsonSerializer.Serialize(ids.ToHashSet()), ct);
    }

        public async Task<bool> MarkOneReadAsync(int userId, string id, string role, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            // xác thực id thuộc phạm vi user hiện tại
            var visible = await _repo.IsVisibleIdAsync(userId, role, id, ct);
            if (!visible) return false;

            var str = await _cache.GetStringAsync(ReadSetKey(userId), ct);
            var set = string.IsNullOrEmpty(str) ? new HashSet<string>() :
                      JsonSerializer.Deserialize<HashSet<string>>(str) ?? new();
            set.Add(id);
            await _cache.SetStringAsync(ReadSetKey(userId), JsonSerializer.Serialize(set), ct);
            return true;
        }
    }
}
