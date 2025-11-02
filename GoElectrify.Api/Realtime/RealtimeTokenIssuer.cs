using System.Text.Json;
using GoElectrify.Api.Common;
using GoElectrify.BLL.Services.Realtime;

namespace GoElectrify.Api.Realtime
{
    public sealed class RealtimeTokenIssuer : IRealtimeTokenIssuer
    {
        private readonly IAblyService _ably;
        private readonly IAblyTokenCache _cache;
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

        public RealtimeTokenIssuer(IAblyService ably, IAblyTokenCache cache)
        {
            _ably = ably; _cache = cache;
        }

        private static string Capability(string ch, bool subscribeOnly, bool allowPresence, bool allowHistory)
        {
            var perms = new List<string> { "subscribe" };
            if (!subscribeOnly) perms.Add("publish");
            if (allowPresence) perms.Add("presence");
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
            var capKey = $"{(subscribeOnly ? "sub" : "subpub")}-{(allowPresence ? "prs" : "-")}-{(allowHistory ? "his" : "-")}";

            var cacheKey = useCache ? $"realtime:session:{sessionId}:client:{clientId}:cap:{capKey}" : null;

            CachedAblyToken? cached = null;
            if (cacheKey is not null) cached = await _cache.GetAsync(cacheKey, ct);

            if (cached is null || cached.ChannelId != channelId || cached.ExpiresAtUtc <= now.AddSeconds(90))
            {
                var token = await _ably.CreateTokenAsync(
                    channelId,
                    clientId,
                    Capability(channelId, subscribeOnly, allowPresence, allowHistory),
                    Ttl,
                    ct);

                cached = new CachedAblyToken
                {
                    ChannelId = channelId,
                    TokenJson = JsonSerializer.Serialize(token, SharedJsonOptions.CamelCase),
                    ExpiresAtUtc = now.Add(Ttl)
                };

                if (cacheKey is not null) await _cache.SaveAsync(cacheKey, cached, Ttl, ct);
            }

            return (JsonSerializer.Deserialize<JsonElement>(cached.TokenJson), cached.ExpiresAtUtc);
        }
    }
}
