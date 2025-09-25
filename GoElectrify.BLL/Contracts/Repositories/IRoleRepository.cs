using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IRoleRepository
    {
        Task<Role> GetByNameAsync(string name, CancellationToken ct);
    }
}
