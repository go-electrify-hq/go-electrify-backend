using GoElectrify.BLL.Services.Realtime;
using System.Text.Json;

namespace GoElectrify.Api.Realtime
{
    public sealed class RealtimeTokenIssuer : IRealtimeTokenIssuer
    {
        private readonly IAblyService _ably;
        private readonly IAblyTokenCache _cache;
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
        private static readonly JsonSerializerOptions Camel = new(JsonSerializerDefaults.Web);

        public RealtimeTokenIssuer(IAblyService ably, IAblyTokenCache cache)
        {
            _ably = ably; _cache = cache;
        }

        private static string Capability(string ch, bool subscribeOnly, bool allowPresence, bool allowHistory)
        {
            var perms = new List<string> { "subscribe" };
            if (allowPresence) perms.Add("presence");
            if (!subscribeOnly) perms.Add("publish");      // chỉ bật khi thật sự cần
            if (allowHistory) perms.Add("history");
            var permsJson = string.Join(",", perms.Select(p => $"\"{p}\""));
            return $@"{{""{ch}"":[{permsJson}]}}";
        }

        public async Task<(JsonElement Token, DateTime ExpiresAtUtc)> IssueAsync(
            int sessionId, string channelId, string clientId,
            bool subscribeOnly = true, bool useCache = true,
            bool allowPresence = false, bool allowHistory = false,
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var key = useCache ? $"realtime:session:{sessionId}:client:{clientId}" : null;

            CachedAblyToken? cached = null;
            if (key is not null) cached = await _cache.GetAsync(key, ct);

            if (cached is null || cached.ChannelId != channelId || cached.ExpiresAtUtc <= now.AddSeconds(90))
            {
                var token = await _ably.CreateTokenAsync(channelId, clientId, Capability(channelId, subscribeOnly, allowPresence, allowHistory), Ttl, ct);
                cached = new CachedAblyToken
                {
                    ChannelId = channelId,
                    TokenJson = JsonSerializer.Serialize(token, Camel),
                    ExpiresAtUtc = now.Add(Ttl)
                };
                if (key is not null) await _cache.SaveAsync(key, cached, Ttl, ct);
            }

            return (JsonSerializer.Deserialize<JsonElement>(cached.TokenJson), cached.ExpiresAtUtc);
        }
    }
}
