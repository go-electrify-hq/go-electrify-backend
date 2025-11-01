using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.StationStaff;
using GoElectrify.BLL.Entities;

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

        public async Task<List<StationStaffDto>> ListAsync(int stationId, CancellationToken ct)
        {
            var station = await _stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var items = await _repo.ListByStationAsync(stationId, includeRevoked: false, ct);
            return items.Select(ToDto).ToList();
        }

        public async Task<StationStaffDto> AssignAsync(int stationId, AssignStaffRequestDto req, CancellationToken ct)
        {
            var station = await _stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            // Lấy user kèm role để kiểm tra staff
            var user = await _userRepo.GetByIdWithRoleAsync(req.UserId, ct);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // CHẶN: chỉ gán người có vai trò "staff"
            var roleName = user.Role?.Name;
            if (string.IsNullOrWhiteSpace(roleName) ||
                !string.Equals(roleName.Trim(), "staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("UserNotStaff");
            }

            // RÀNG BUỘC: 1 staff chỉ thuộc 1 trạm (active) trên toàn hệ thống
            var currentActive = await _repo.GetActiveByUserAsync(req.UserId, ct);
            if (currentActive != null && currentActive.StationId != stationId)
                throw new InvalidOperationException($"StaffAssignedToOtherStation:{currentActive.StationId}");

            // Idempotent: nếu đã tồn tại và đang active -> báo lỗi; nếu đã revoke -> khôi phục
            var existed = await _repo.GetAsync(stationId, req.UserId, ct);
            if (existed is { RevokedAt: null })
                throw new InvalidOperationException("StaffAlreadyAssigned");

            if (existed is { RevokedAt: not null })
            {
                existed.RevokedAt = null;
                existed.RevokedReason = null;
                existed.AssignedAt = DateTime.UtcNow;
                _repo.Update(existed);
                await _repo.SaveAsync(ct);
                return ToDto(existed);
            }

            // Tạo mới
            var entity = new StationStaff
            {
                StationId = stationId,
                UserId = req.UserId,
                AssignedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity, ct);
            await _repo.SaveAsync(ct);

            var saved = await _repo.GetAsync(stationId, req.UserId, ct) ?? entity;
            return ToDto(saved);
        }

        public async Task<RevokeStaffResultDto> DeleteAsync(int stationId, int userId, string reason, CancellationToken ct)
        {
            reason = (reason ?? string.Empty).Trim();
            if (reason.Length < 3) throw new ArgumentException("Revoke reason must be at least 3 characters.", nameof(reason));

            var existed = await _repo.GetAsync(stationId, userId, ct);
            if (existed == null) throw new KeyNotFoundException("Assignment not found.");

            if (existed.RevokedAt != null)
            {
                return new RevokeStaffResultDto
                {
                    StationId = stationId,
                    UserId = userId,
                    Action = "noop_already_revoked",
                    RevokedAt = existed.RevokedAt,
                    RevokedReason = existed.RevokedReason
                };
            }

            existed.RevokedAt = DateTime.UtcNow;
            existed.RevokedReason = reason;
            _repo.Update(existed);
            await _repo.SaveAsync(ct);

            return new RevokeStaffResultDto
            {
                StationId = stationId,
                UserId = userId,
                Action = "revoked",
                RevokedAt = existed.RevokedAt,
                RevokedReason = existed.RevokedReason
            };
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
