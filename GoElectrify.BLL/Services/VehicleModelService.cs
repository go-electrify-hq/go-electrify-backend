using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.VehicleModels;
using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class VehicleModelService(IVehicleModelRepository repo) : IVehicleModelService
    {
         public async Task<List<VehicleModelDto>> ListAsync(string? search, CancellationToken ct)
        {
            var entities = await repo.ListAsync(search, ct);
            return entities.Select(x => new VehicleModelDto
            {
                Id = x.Id,
                ModelName = x.ModelName,
                MaxPowerKw = x.MaxPowerKw,
                BatteryCapacityKwh = x.BatteryCapacityKwh,
                ConnectorTypeIds = x.VehicleModelConnectorTypes.Select(j => j.ConnectorTypeId).ToList(),
            }).ToList();
        }

        public async Task<VehicleModelDto?> GetAsync(int id, CancellationToken ct)
        {
            var vm = await repo.GetDetailAsync(id, ct);
            if (vm == null) return null;

            return new VehicleModelDto
            {
                Id = vm.Id,
                ModelName = vm.ModelName,
                MaxPowerKw = vm.MaxPowerKw,
                BatteryCapacityKwh = vm.BatteryCapacityKwh,
                ConnectorTypeIds = vm.VehicleModelConnectorTypes.Select(j => j.ConnectorTypeId).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateVehicleModelDto dto, CancellationToken ct)
        {
            if (await repo.ExistsByNameAsync(dto.ModelName, null, ct))
                throw new InvalidOperationException("ModelName already exists.");

            if (dto.ConnectorTypeIds is { Count: > 0 } &&
                !await repo.AllConnectorTypesExistAsync(dto.ConnectorTypeIds, ct))
                throw new InvalidOperationException("Some ConnectorTypeIds do not exist.");

            var entity = new VehicleModel
            {
                ModelName = dto.ModelName.Trim(),
                MaxPowerKw = dto.MaxPowerKw,
                BatteryCapacityKwh = dto.BatteryCapacityKwh
            };

            await repo.AddAsync(entity, ct);
            await repo.SaveAsync(ct); // sinh Id

            if (dto.ConnectorTypeIds is { Count: > 0 })
            {
                await repo.AddJoinsAsync(entity.Id, dto.ConnectorTypeIds!, ct);
                await repo.SaveAsync(ct);
            }

            return entity.Id;
        }

        public async Task UpdateAsync(int id, UpdateVehicleModelDto dto, CancellationToken ct)
        {
            var vm = await repo.GetDetailAsync(id, ct) ?? throw new KeyNotFoundException("VehicleModel not found.");

            if (!string.Equals(vm.ModelName, dto.ModelName, StringComparison.OrdinalIgnoreCase) &&
                await repo.ExistsByNameAsync(dto.ModelName, id, ct))
                throw new InvalidOperationException("ModelName already exists.");

            if (dto.ConnectorTypeIds is { Count: > 0 } &&
                !await repo.AllConnectorTypesExistAsync(dto.ConnectorTypeIds, ct))
                throw new InvalidOperationException("Some ConnectorTypeIds do not exist.");

            vm.ModelName = dto.ModelName.Trim();
            vm.MaxPowerKw = dto.MaxPowerKw;
            vm.BatteryCapacityKwh = dto.BatteryCapacityKwh;

            await repo.RemoveAllJoinsAsync(vm.Id, ct);
            if (dto.ConnectorTypeIds is { Count: > 0 })
                await repo.AddJoinsAsync(vm.Id, dto.ConnectorTypeIds!, ct);

            await repo.SaveAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var vm = await repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("VehicleModel not found.");
            repo.Remove(vm);
            await repo.SaveAsync(ct);
        }
    }
}
