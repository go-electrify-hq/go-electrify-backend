using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken ct);
        Task AddAsync(User user, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
        Task<User?> GetByIdAsync(int id, CancellationToken ct);
        Task<User?> GetDetailAsync(int id, CancellationToken ct); // Include Role & Wallet
        Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
            string? role, string? search, string? sort, int page, int pageSize, CancellationToken ct);
    }
}
