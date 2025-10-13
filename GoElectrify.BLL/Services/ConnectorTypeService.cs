using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ConnectorTypes;
using GoElectrify.BLL.Dtos.ConnectorTypes;
using GoElectrify.BLL.Dtos.VehicleModels;
using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
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
            // Blocked chỉ tính Chargers + Bookings
            var blocked = await repo.FindBlockedIdsAsync(new[] { id }, ct);
            if (blocked.Contains(id))
                throw new InvalidOperationException("Cannot delete ConnectorType: referenced by chargers/bookings.");

            // Không blocked → dọn join M-N và xoá
            await repo.RemoveAllJoinsAsync(id, ct);

            var entity = await repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("ConnectorType not found.");
            repo.Remove(entity);
            await repo.SaveAsync(ct);
        }

        // ===== DELETE (batch, all-or-nothing) =====
        public async Task<DeleteConnectorTypeResultDto> DeleteManyWithReportAsync(List<int> ids, CancellationToken ct)
        {
            if (ids is null || ids.Count == 0)
                throw new ArgumentException("Ids must not be empty.", nameof(ids));

            var input = ids.Where(x => x > 0).Distinct().ToList();

            // NotFound?
            var existing = await repo.GetExistingIdsAsync(input, ct);
            var notFound = input.Where(x => !existing.Contains(x)).OrderBy(x => x).ToList();
            if (notFound.Count > 0)
            {
                return new DeleteConnectorTypeResultDto
                {
                    Deleted = 0,
                    DeletedIds = new(),
                    BlockedIds = new(),
                    NotFoundIds = notFound
                };
            }

            // Blocked? (Chargers + Bookings)
            var blocked = await repo.FindBlockedIdsAsync(existing, ct);
            if (blocked.Count > 0)
            {
                return new DeleteConnectorTypeResultDto
                {
                    Deleted = 0,
                    DeletedIds = new(),
                    BlockedIds = blocked.OrderBy(x => x).ToList(),
                    NotFoundIds = new()
                };
            }

            // Sạch → repo lo transaction + retry (dọn join + xoá parent)
            var deleted = await repo.DeleteManySafeAsync(existing, ct);

            return new DeleteConnectorTypeResultDto
            {
                Deleted = deleted,
                DeletedIds = existing.OrderBy(x => x).ToList(),
                BlockedIds = new(),
                NotFoundIds = new()
            };
        }
    }
}
