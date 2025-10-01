using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Subscription;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public sealed class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repo;
        public SubscriptionService(ISubscriptionRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<SubscriptionDto>> GetAllAsync(CancellationToken ct) =>
            (await _repo.GetAllAsync(ct)).Select(Map).ToList();

        public async Task<SubscriptionDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var s = await _repo.GetByIdAsync(id, ct);
            return s is null ? null : Map(s);
        }

        public async Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Name is required");
            if (dto.Price < 0) throw new ArgumentException("Price must be >= 0");
            if (dto.TotalKwh < 0) throw new ArgumentException("TotalKwh must be >= 0");
            if (dto.DurationDays <= 0) throw new ArgumentException("DurationDays must be > 0");

            if (await _repo.NameExistsAsync(dto.Name, null, ct))
                throw new InvalidOperationException("Subscription name already exists");

            var now = DateTime.UtcNow;
            var s = new Subscription
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                TotalKwh = dto.TotalKwh,
                DurationDays = dto.DurationDays,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repo.AddAsync(s, ct);
            return Map(s);
        }

        public async Task<SubscriptionDto?> UpdateAsync(int id, SubscriptionUpdateDto dto, CancellationToken ct)
        {
            var s = await _repo.GetByIdAsync(id, ct);
            if (s is null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                if (await _repo.NameExistsAsync(dto.Name, id, ct))
                    throw new InvalidOperationException("Subscription name already exists");
                s.Name = dto.Name.Trim();
            }
            if (dto.Price is not null)
            {
                if (dto.Price < 0) throw new ArgumentException("Price must be >= 0");
                s.Price = dto.Price.Value;
            }
            if (dto.TotalKwh is not null)
            {
                if (dto.TotalKwh < 0) throw new ArgumentException("TotalKwh must be >= 0");
                s.TotalKwh = dto.TotalKwh.Value;
            }
            if (dto.DurationDays is not null)
            {
                if (dto.DurationDays <= 0) throw new ArgumentException("DurationDays must be > 0");
                s.DurationDays = dto.DurationDays.Value;
            }

            s.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(s, ct);
            return Map(s);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken ct) => _repo.DeleteAsync(id, ct);

        private static SubscriptionDto Map(Subscription s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Price = s.Price,
            TotalKwh = s.TotalKwh,
            DurationDays = s.DurationDays,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
