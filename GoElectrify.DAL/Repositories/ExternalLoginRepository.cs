using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class ExternalLoginRepository(AppDbContext db) : IExternalLoginRepository
    {
        public Task<ExternalLogin?> FindAsync(string provider, string providerUserId, CancellationToken ct)
            => db.Set<ExternalLogin>()
                 .Include(x => x.User)
                 .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == providerUserId, ct);

        public Task AddAsync(ExternalLogin login, CancellationToken ct)
            => db.Set<ExternalLogin>().AddAsync(login, ct).AsTask();

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
        public Task<bool> ExistsAsync(int userId, string provider, string providerUserId, CancellationToken ct)
                => db.ExternalLogins.AnyAsync(x =>
                    x.UserId == userId &&
                    x.Provider == provider &&
                    x.ProviderUserId == providerUserId, ct);
    }
}
