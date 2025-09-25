namespace GoElectrify.BLL.Contracts.Services
{
    public interface IRedisCache
    {
        /// <summary>SET key với TTL (dùng cho OTP, rate-limit, cache).</summary>
        Task<bool> SetAsync(string key, string value, TimeSpan? ttl = null);

        /// <summary>Lấy giá trị string từ key.</summary>
        Task<string?> GetAsync(string key);

        /// <summary>Xoá key.</summary>
        Task<bool> DeleteAsync(string key);

        /// <summary>Tăng counter; nếu key mới tạo thì set TTL.</summary>
        Task<long> IncrAsync(string key, TimeSpan? ttlWhenCreate = null);
    }
}
