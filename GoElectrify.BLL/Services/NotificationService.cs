using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using Microsoft.Extensions.Caching.Distributed;

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

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardAsync(
            NotificationQueryDto query, int userId, string role, CancellationToken cancellationToken)
        {
            // Lấy danh sách thô
            var items = await _repository.GetDashboardBaseAsync(query, userId, role, cancellationToken);

            // Đọc LastSeen từ cache (viết thẳng; không dùng helper)
            string key = "notif:lastseen:" + userId;
            byte[]? bytes = await _cache.GetAsync(key, cancellationToken);
            DateTime? lastSeen = null;
            if (bytes != null)
            {
                string str = System.Text.Encoding.UTF8.GetString(bytes);
                DateTime parsed;
                bool ok = DateTime.TryParse(str, out parsed);
                if (ok) lastSeen = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            // Gắn IsNew
            if (lastSeen == null)
            {
                foreach (var n in items) n.IsNew = true;
            }
            else
            {
                foreach (var n in items)
                {
                    if (n.CreatedAt > lastSeen.Value) n.IsNew = true;
                    else n.IsNew = false;
                }
            }

            return items;
        }

        public async Task<DateTime> MarkAllReadNowAsync(int userId, CancellationToken cancellationToken)
        {
            // Lưu LastSeen = Now (UTC) vào cache (viết thẳng; không helper)
            DateTime nowUtc = DateTime.UtcNow;
            string key = "notif:lastseen:" + userId;
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(nowUtc.ToString("O"));

            var options = new DistributedCacheEntryOptions();
            options.SlidingExpiration = TimeSpan.FromDays(30);

            await _cache.SetAsync(key, payload, options, cancellationToken);
            return nowUtc;
        }
    }
}
