using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IChargerRepository
    {
        Task<List<Charger>> GetAllAsync(CancellationToken ct);
        Task<Charger?> GetByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Charger entity, CancellationToken ct);
        Task UpdateAsync(Charger entity, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);

        Task<bool> CodeExistsAsync(int stationId, string code, int? exceptId, CancellationToken ct);
    }
}
