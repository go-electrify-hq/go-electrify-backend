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
    public class StationStaffService : IStationStaffService
    {
        private readonly IStationRepository _stationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IStationStaffRepository _repo;

        public StationStaffService(
            IStationRepository stationRepo,
            IUserRepository userRepo,
            IStationStaffRepository repo)
        {
            _stationRepo = stationRepo;
            _userRepo = userRepo;
            _repo = repo;
        }

        public async Task<List<StationStaffDto>> ListAsync(int stationId)
        {
            var station = await _stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            // Vì không còn revoke, trả toàn bộ bản ghi của station
            var items = await _repo.ListByStationAsync(stationId, includeRevoked: true);
            return items.Select(ToDto).ToList();
        }

        public async Task<StationStaffDto> AssignAsync(int stationId, AssignStaffRequestDto req)
        {
            var station = await _stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var user = await _userRepo.GetByIdAsync(req.UserId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var existed = await _repo.GetAsync(stationId, req.UserId);
            if (existed != null)
                throw new InvalidOperationException("User already assigned to this station.");

            var entity = new StationStaff
            {
                StationId = stationId,
                UserId = req.UserId,
                Role = NormalizeRole(req.Role),
                AssignedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);

            var saved = await _repo.GetAsync(stationId, req.UserId) ?? entity;
            return ToDto(saved);
        }

        public async Task<bool> RemoveAsync(int stationId, int userId)
        {
            var existed = await _repo.GetAsync(stationId, userId);
            if (existed == null) return false;
            await _repo.RemoveAsync(existed);
            return true;
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
            Role = s.Role,
            AssignedAt = s.AssignedAt
        };
    }
}
