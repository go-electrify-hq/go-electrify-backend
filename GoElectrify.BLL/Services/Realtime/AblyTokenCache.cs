using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GoElectrify.BLL.Common;
using Microsoft.Extensions.Caching.Distributed;

namespace GoElectrify.BLL.Services.Realtime
{
    public sealed class CachedAblyToken
    {
        public string ChannelId { get; set; } = default!;
        public string TokenJson { get; set; } = default!;     // serialize tokenDetails
        public DateTime ExpiresAtUtc { get; set; }
    }

    public interface IAblyTokenCache
    {
        Task SaveAsync(string key, CachedAblyToken token, TimeSpan ttl, CancellationToken ct);
        Task<CachedAblyToken?> GetAsync(string key, CancellationToken ct);
    }

    public sealed class AblyTokenCache : IAblyTokenCache
    {
        private readonly IDistributedCache _cache;

        public AblyTokenCache(IDistributedCache cache) => _cache = cache;

        public Task SaveAsync(string key, CachedAblyToken token, TimeSpan ttl, CancellationToken ct)
            => _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(token, SharedJsonOptions.CamelCase),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                ct);

        public async Task<CachedAblyToken?> GetAsync(string key, CancellationToken ct)
        {
            var raw = await _cache.GetStringAsync(key, ct);
            return string.IsNullOrEmpty(raw) ? null : JsonSerializer.Deserialize<CachedAblyToken>(raw, SharedJsonOptions.CamelCase);
        }
    }
}
