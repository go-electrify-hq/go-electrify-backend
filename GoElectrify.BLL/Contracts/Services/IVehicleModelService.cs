using GoElectrify.BLL.Dto.VehicleModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IVehicleModelService
    {
        Task<List<VehicleModelDto>> ListAsync(string? search, CancellationToken ct);
        Task<VehicleModelDto?> GetAsync(int id, CancellationToken ct);
        Task<int> CreateAsync(CreateVehicleModelDto dto, CancellationToken ct);
        Task UpdateAsync(int id, UpdateVehicleModelDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);

    }
}
