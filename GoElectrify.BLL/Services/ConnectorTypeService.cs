using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ConnectorTypes;
using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class ConnectorTypeService(IConnectorTypeRepository repo) : IConnectorTypeService
    {
        public async Task<List<ConnectorTypeDto>> ListAsync(string? search, CancellationToken ct)
        {
            var items = await repo.ListAsync(search, ct);
            return items.Select(x => new ConnectorTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                MaxPowerKw = x.MaxPowerKw
            }).ToList();
        }

        public async Task<ConnectorTypeDto?> GetAsync(int id, CancellationToken ct)
        {
            var ctEntity = await repo.GetByIdAsync(id, ct);
            if (ctEntity == null) return null;

            return new ConnectorTypeDto
            {
                Id = ctEntity.Id,
                Name = ctEntity.Name,
                Description = ctEntity.Description,
                MaxPowerKw = ctEntity.MaxPowerKw
            };
        }

        public async Task<int> CreateAsync(CreateConnectorTypeDto dto, CancellationToken ct)
        {
            if (await repo.ExistsByNameAsync(dto.Name, null, ct))
                throw new InvalidOperationException("Name already exists.");

            var entity = new ConnectorType
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                MaxPowerKw = dto.MaxPowerKw
            };

            await repo.AddAsync(entity, ct);
            await repo.SaveAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(int id, UpdateConnectorTypeDto dto, CancellationToken ct)
        {
            var entity = await repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("ConnectorType not found.");

            if (!string.Equals(entity.Name, dto.Name, StringComparison.OrdinalIgnoreCase) &&
                await repo.ExistsByNameAsync(dto.Name, id, ct))
                throw new InvalidOperationException("Name already exists.");

            entity.Name = dto.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.MaxPowerKw = dto.MaxPowerKw;

            await repo.SaveAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var entity = await repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("ConnectorType not found.");

            // Nếu có quan hệ với VehicleModel → xóa join trước để tránh lỗi FK
            if (await repo.HasAnyJoinAsync(id, ct))
                await repo.RemoveAllJoinsAsync(id, ct);

            repo.Remove(entity);
            await repo.SaveAsync(ct);
        }
    }
}
