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

        private static string Capability(string ch, bool subscribeOnly) =>
            subscribeOnly
                ? $@"{{""{ch}"":[""subscribe""]}}"
                : $@"{{""{ch}"":[""subscribe"",""publish""]}}";

        public async Task<(JsonElement Token, DateTime ExpiresAtUtc)> IssueAsync(
            int sessionId, string channelId, string clientId, bool subscribeOnly = true, bool useCache = true, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var key = useCache ? $"realtime:session:{sessionId}:client:{clientId}" : null;

            CachedAblyToken? cached = null;
            if (key is not null) cached = await _cache.GetAsync(key, ct);

            if (cached is null || cached.ChannelId != channelId || cached.ExpiresAtUtc <= now.AddSeconds(90))
            {
                var token = await _ably.CreateTokenAsync(channelId, clientId, Capability(channelId, subscribeOnly), Ttl, ct);
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
