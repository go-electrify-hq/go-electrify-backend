using System.Text.Json;

namespace GoElectrify.Api.Realtime
{
    public interface IRealtimeTokenIssuer
    {
        Task<(JsonElement Token, DateTime ExpiresAtUtc)> IssueAsync(
            int sessionId,
            string channelId,
            string clientId,
            bool subscribeOnly = true,   // “giống nhau” cho FE = subscribe-only
            bool useCache = true,
            CancellationToken ct = default);
    }
}
