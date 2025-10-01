using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public sealed class ChargerService : IChargerService
    {
        private readonly IChargerRepository _repo;
        public ChargerService(IChargerRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<ChargerDto>> GetAllAsync(CancellationToken ct)
            => (await _repo.GetAllAsync(ct)).Select(Map).ToList();

        public async Task<ChargerDto?> GetByIdAsync(int id, CancellationToken ct)
            => (await _repo.GetByIdAsync(id, ct)) is { } e ? Map(e) : null;

        public async Task<ChargerDto> CreateAsync(ChargerCreateDto dto, CancellationToken ct)
        {
            Validate(dto);

            if (await _repo.CodeExistsAsync(dto.StationId, dto.Code, null, ct))
                throw new InvalidOperationException("Charger code already exists in this station.");

            var now = DateTime.UtcNow;
            var e = new Charger
            {
                StationId = dto.StationId,
                ConnectorTypeId = dto.ConnectorTypeId,
                Code = dto.Code.Trim(),
                PowerKw = dto.PowerKw,
                Status = dto.Status.Trim(),
                PricePerKwh = dto.PricePerKwh,
            };
            await _repo.AddAsync(e, ct);
            return Map(e);
        }

        public async Task<ChargerDto?> UpdateAsync(int id, ChargerUpdateDto dto, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(id, ct);
            if (e is null) return null;
            if (dto.ConnectorTypeId is not null) e.ConnectorTypeId = dto.ConnectorTypeId.Value;
            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                if (await _repo.CodeExistsAsync(e.StationId, dto.Code, id, ct))
                    throw new InvalidOperationException("Charger code already exists in this station.");
                e.Code = dto.Code.Trim();
            }
            if (dto.PowerKw is not null) e.PowerKw = dto.PowerKw.Value;
            if (!string.IsNullOrWhiteSpace(dto.Status)) e.Status = dto.Status.Trim();
            if (dto.PricePerKwh is not null) e.PricePerKwh = dto.PricePerKwh.Value;

            e.UpdatedAt = DateTime.UtcNow;
            Validate(e);
            await _repo.UpdateAsync(e, ct);
            return Map(e);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken ct)
            => _repo.DeleteAsync(id, ct);

        private static void Validate(ChargerCreateDto dto)
        {
            if (dto.StationId <= 0) throw new ArgumentException("StationId is required");
            if (dto.ConnectorTypeId <= 0) throw new ArgumentException("ConnectorTypeId is required");
            if (string.IsNullOrWhiteSpace(dto.Code)) throw new ArgumentException("Code is required");
            if (dto.PowerKw <= 0) throw new ArgumentException("PowerKw must be > 0");
            if (string.IsNullOrWhiteSpace(dto.Status)) throw new ArgumentException("Status is required");
        }

        private static void Validate(Charger e)
        {
            if (e.PowerKw <= 0) throw new ArgumentException("PowerKw must be > 0");
            if (string.IsNullOrWhiteSpace(e.Code)) throw new ArgumentException("Code is required");
            if (string.IsNullOrWhiteSpace(e.Status)) throw new ArgumentException("Status is required");
        }

        private static ChargerDto Map(Charger e) => new()
        {
            Id = e.Id,
            StationId = e.StationId,
            ConnectorTypeId = e.ConnectorTypeId,
            Code = e.Code,
            PowerKw = e.PowerKw,
            Status = e.Status,
            PricePerKwh = e.PricePerKwh,
        };
    }
}
