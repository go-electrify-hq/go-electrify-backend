using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.StationStaff;
using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class StationStaffService(
        IStationRepository stationRepo,
        IUserRepository userRepo,
        IStationStaffRepository repo) : IStationStaffService
    {
        public async Task<List<StationStaffDto>> ListAsync(int stationId, CancellationToken ct)
        {
            // station phải tồn tại
            var station = await stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var items = await repo.ListByStationAsync(stationId, ct);
            return items.Select(ToDto).ToList();
        }

        public async Task<StationStaffDto> AssignAsync(int stationId, AssignStaffRequestDto req, CancellationToken ct)
        {
            // station & user phải tồn tại
            var station = await stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var user = await userRepo.GetByIdAsync(req.UserId, ct);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // không cho trùng (StationId, UserId)
            var existed = await repo.GetAsync(stationId, req.UserId, ct);
            if (existed != null) throw new InvalidOperationException("User already assigned to this station.");

            if (existed is { RevokedAt: not null })
            {
                // Re-activate
                existed.RevokedAt = null;
                existed.AssignedAt = DateTime.UtcNow;
                repo.Update(existed);
                await repo.SaveAsync(ct);
                return ToDto(existed);
            }

            var entity = new StationStaff
            {
                StationId = stationId,
                UserId = req.UserId,
                AssignedAt = DateTime.UtcNow,
                RevokedAt = null
            };

            await repo.AddAsync(entity, ct);
            await repo.SaveAsync(ct);

            var saved = await repo.GetAsync(stationId, req.UserId, ct) ?? entity;
            return ToDto(saved);
        }

        public async Task DeleteAsync(int stationId, int userId, string reason, CancellationToken ct)
        {
            reason = (reason ?? string.Empty).Trim();
            if (reason.Length < 3) throw new ArgumentException("Revoke reason must be at least 3 characters.", nameof(reason));

            var existed = await repo.GetAsync(stationId, userId, ct);
            if (existed == null) throw new KeyNotFoundException("Assignment not found.");

            if (existed.RevokedAt != null) return; // idempotent

            existed.RevokedAt = DateTime.UtcNow;
            existed.RevokedReason = reason;
            repo.Update(existed);
            await repo.SaveAsync(ct);
        }

        private static string NormalizeRole(string? role)
        {
            role = (role ?? "STAFF").Trim().ToUpperInvariant();
            return role is "MANAGER" or "STAFF" ? role : "STAFF";
        }

        private static StationStaffDto ToDto(StationStaff s) => new()
        {
            StationId = s.StationId,
            UserId = s.UserId,
            UserEmail = s.User?.Email,
            UserFullName = s.User?.FullName,
            AssignedAt = s.AssignedAt,
            RevokedAt = s.RevokedAt
        };
    }
}
