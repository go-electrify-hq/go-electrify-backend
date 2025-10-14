using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<List<Subscription>> GetAllAsync(CancellationToken ct);
        Task<Subscription?> GetByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Subscription sub, CancellationToken ct);
        Task UpdateAsync(Subscription sub, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);
        Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);
    }
}
