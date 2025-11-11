using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IExternalLoginRepository
    {
        Task<ExternalLogin?> FindAsync(string provider, string providerUserId, CancellationToken ct);
        Task AddAsync(ExternalLogin login, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
        Task<bool> ExistsAsync(int userId, string provider, string providerUserId, CancellationToken ct);
    }
}
