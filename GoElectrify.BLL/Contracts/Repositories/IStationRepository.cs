using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IStationRepository
    {
        Task<IEnumerable<Station>> GetAllAsync();
        Task<Station?> GetByIdAsync(int id);
        Task AddAsync(Station station);
        Task UpdateAsync(Station station);
        Task DeleteAsync(Station station);
    }
}