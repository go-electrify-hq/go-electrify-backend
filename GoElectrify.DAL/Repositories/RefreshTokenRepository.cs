using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
    {
        public Task AddAsync(RefreshToken token, CancellationToken ct)
            => db.RefreshTokens.AddAsync(token, ct).AsTask();

        public Task<RefreshToken?> FindActiveAsync(int userId, string tokenHash, CancellationToken ct)
            => db.RefreshTokens
                 .FirstOrDefaultAsync(x =>
                     x.UserId == userId &&
                     x.TokenHash == tokenHash &&
                     x.RevokedAt == null &&
                     x.ExpiresAt > DateTime.UtcNow, ct);

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
