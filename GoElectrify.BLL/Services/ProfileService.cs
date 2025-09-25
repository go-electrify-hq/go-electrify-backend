using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task UpdateFullNameAsync(int userId, string? fullName, CancellationToken ct)
        {
            var u = await users.GetByIdAsync(userId, ct)
                    ?? throw new InvalidOperationException("User not found");
            u.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            u.UpdatedAt = DateTime.UtcNow;
            await users.SaveAsync(ct);
        }
    }
}
