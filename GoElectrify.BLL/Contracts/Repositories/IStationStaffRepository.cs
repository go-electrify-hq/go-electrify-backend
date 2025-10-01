using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IStationStaffRepository
    {
        Task<StationStaff?> GetAsync(int stationId, int staffUserId);
        Task AddAsync(StationStaff entity);
        Task UpdateAsync(StationStaff entity);
        Task<List<StationStaff>> GetStaffByStationAsync(int stationId, bool onlyActive);
        Task<List<StationStaff>> GetStationsByStaffAsync(int staffUserId, bool onlyActive);
        Task<bool> ExistsActiveAsync(int stationId, int staffUserId);
        Task<bool> AnyPrimaryAsync(int stationId);
        Task SaveChangesAsync();
    }
}
