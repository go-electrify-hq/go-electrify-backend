using GoElectrify.BLL.Contracts.Services;
using StackExchange.Redis;

namespace GoElectrify.DAL.Infra
{
    public class RedisCache(IConnectionMultiplexer mux) : IRedisCache
    {
        private readonly IDatabase _db = mux.GetDatabase();

        public Task<bool> SetAsync(string key, string value, TimeSpan? ttl = null)
            => _db.StringSetAsync(key, value, ttl);

        public async Task<string?> GetAsync(string key)
        {
            var v = await _db.StringGetAsync(key);
            return v.HasValue ? v.ToString() : null;
        }

        public Task<bool> DeleteAsync(string key) => _db.KeyDeleteAsync(key);

        public async Task<long> IncrAsync(string key, TimeSpan? ttlWhenCreate = null)
        {
            var created = !await _db.KeyExistsAsync(key);
            var val = await _db.StringIncrementAsync(key);
            if (created && ttlWhenCreate is not null) await _db.KeyExpireAsync(key, ttlWhenCreate);
            return val;
        }
    }
}
