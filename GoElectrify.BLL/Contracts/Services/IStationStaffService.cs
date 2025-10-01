using GoElectrify.BLL.Dto.StationStaff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IStationStaffService
    {
        Task<List<StationStaffDto>> ListAsync(int stationId);
        Task<StationStaffDto> AssignAsync(int stationId, AssignStaffRequestDto req);
        Task<bool> RemoveAsync(int stationId, int userId); // hard delete
    }
}
