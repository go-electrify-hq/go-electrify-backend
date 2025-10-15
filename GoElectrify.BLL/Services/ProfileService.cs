using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;

namespace GoElectrify.BLL.Services
{
    public sealed class ProfileService(IUserRepository users) : IProfileService
    {
        public async Task<object> GetMeAsync(int userId, CancellationToken ct)
        {
            var u = await users.GetDetailAsync(userId, ct)
                    ?? throw new InvalidOperationException("User not found");
            return new
            {
                u.Id,
                u.Email,
                u.FullName,
                Role = u.Role?.Name ?? "Driver",
                WalletBalance = u.Wallet?.Balance ?? 0m
            };
        }

        public async Task UpdateProfileAsync(int userId, string? fullName, string? avatarUrl, CancellationToken ct)
        {
            var u = await users.GetByIdAsync(userId, ct)
                    ?? throw new InvalidOperationException("User not found");
            u.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            u.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
            u.UpdatedAt = DateTime.UtcNow;
            await users.SaveAsync(ct);
        }

        private static string? Normalize(string? s)
    => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        public async Task UpdateFullNameAsync(int userId, string? fullName, CancellationToken ct)
        {
            var u = await users.GetByIdAsync(userId, ct)
                    ?? throw new InvalidOperationException("User not found");

            var newName = Normalize(fullName);

            if (!string.Equals(u.FullName, newName, StringComparison.Ordinal))
            {
                u.FullName = newName;
                u.UpdatedAt = DateTime.UtcNow;
                await users.SaveAsync(ct);
            }
        }

        public async Task UpdateAvatarAsync(int userId, string? avatarUrl, CancellationToken ct)
        {
            var u = await users.GetByIdAsync(userId, ct)
                    ?? throw new InvalidOperationException("User not found");

            string? newUrl = Normalize(avatarUrl);

            if (newUrl is not null)
            {
                // kiểm tra nhẹ URL hợp lệ
                if (!Uri.TryCreate(newUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException("Avatar URL is invalid.");
                }
            }

            if (!string.Equals(u.AvatarUrl, newUrl, StringComparison.Ordinal))
            {
                u.AvatarUrl = newUrl;
                u.UpdatedAt = DateTime.UtcNow;
                await users.SaveAsync(ct);
            }
        }

    }
}
