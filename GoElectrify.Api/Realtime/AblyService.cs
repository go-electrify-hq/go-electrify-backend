using IO.Ably;

namespace GoElectrify.Api.Realtime
{
    public interface IAblyService
    {
        Task<string> CreateTokenAsync(string channel, string clientId, string capabilityJson, TimeSpan ttl, CancellationToken ct);
        Task PublishAsync(string channel, string name, object payload, CancellationToken ct);
    }

    public sealed class AblyService : IAblyService
    {
        private readonly AblyRest _rest;
        public AblyService(IConfiguration cfg) => _rest = new AblyRest(cfg["Ably:ApiKey"]!);

        public async Task<string> CreateTokenAsync(string channel, string clientId, string capabilityJson, TimeSpan ttl, CancellationToken ct)
        {
            var tokenParams = new TokenParams
            {
                ClientId = clientId,
                Ttl = ttl,
                Capability = new Capability(capabilityJson)
            };

            var tokenDetails = await _rest.Auth.RequestTokenAsync(tokenParams);
            return tokenDetails.Token;
        }

        public Task PublishAsync(string channel, string name, object payload, CancellationToken ct)
            => _rest.Channels.Get(channel).PublishAsync(name, payload);
    }
}
