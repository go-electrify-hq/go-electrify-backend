using System.Text.Json;

namespace GoElectrify.Api.Realtime
{
    public interface IRealtimeTokenIssuer
    {
        Task<(JsonElement Token, DateTime ExpiresAtUtc)> IssueAsync(
            int sessionId, string channelId, string clientId,
            bool subscribeOnly = true, bool useCache = true,
            bool allowPresence = false, bool allowHistory = false,
            CancellationToken ct = default);
    }
}
