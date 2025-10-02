using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Incidents;
using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class IncidentService(
        IStationRepository stationRepo,
        IUserRepository userRepo,
        IStationStaffRepository staffRepo,
        IIncidentRepository repo) : IIncidentService
    {
        public async Task<IncidentDto> CreateAsync(int stationId, int reporterUserId, IncidentCreateDto dto, CancellationToken ct)
        {
            // station & reporter phải hợp lệ
            var station = await stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var user = await userRepo.GetByIdAsync(reporterUserId, ct);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // yêu cầu Staff phải được assign vào station
            var staff = await staffRepo.GetAsync(stationId, reporterUserId, ct);
            if (staff == null || staff.RevokedAt != null)
                throw new InvalidOperationException("You are not assigned to this station.");

            var now = DateTime.UtcNow;

            var entity = new Incident
            {
                StationId = stationId,
                ChargerId = dto.ChargerId,
                ReportedByUserId = reporterUserId,
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Severity = (dto.Severity ?? "LOW").ToUpperInvariant(),
                Status = "OPEN",
                ReportedAt = dto.ReportedAt ?? now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await repo.AddAsync(entity, ct);
            return ToDto(entity);
        }

        public async Task<List<IncidentDto>> ListAsync(int stationId, IncidentListQueryDto query, CancellationToken ct)
        {
            var station = await stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            var list = await repo.ListByStationAsync(
                stationId,
                query.Status?.ToUpperInvariant(),
                query.Severity?.ToUpperInvariant(),
                query.FromReportedAt,
                query.ToReportedAt,
                ct);

            return list.Select(ToDto).ToList();
        }

        public async Task<IncidentDto> GetAsync(int stationId, int incidentId, CancellationToken ct)
        {
            var incident = await repo.GetByIdAsync(incidentId, ct);
            if (incident == null || incident.StationId != stationId)
                throw new KeyNotFoundException("Incident not found.");
            return ToDto(incident);
        }

        public async Task<IncidentDto> UpdateStatusAsync(int stationId, int incidentId, int userId, IncidentUpdateStatusDto dto, CancellationToken ct)
        {
            var incident = await repo.GetByIdAsync(incidentId, ct);
            if (incident == null || incident.StationId != stationId)
                throw new KeyNotFoundException("Incident not found.");

            // chỉ staff được assign hoặc admin mới được cập nhật
            var staff = await staffRepo.GetAsync(stationId, userId, ct);
            var isAssignedStaff = staff != null && staff.RevokedAt == null;

            // nếu không phải staff assigned thì vẫn có thể là Admin — check rất nhẹ: role lấy từ user
            var user = await userRepo.GetByIdAsync(userId, ct);
            var isAdmin = (user?.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false);

            if (!isAssignedStaff && !isAdmin)
                throw new InvalidOperationException("You are not allowed to update this incident.");

            var newStatus = dto.Status.ToUpperInvariant();
            incident.Status = newStatus;

            if ((newStatus == "RESOLVED" || newStatus == "CLOSED"))
                incident.ResolvedAt = dto.ResolvedAt ?? DateTime.UtcNow;

            incident.UpdatedAt = DateTime.UtcNow;

            repo.Update(incident);
            await repo.SaveAsync(ct);

            return ToDto(incident);
        }

        private static IncidentDto ToDto(Incident x) => new()
        {
            Id = x.Id,
            StationId = x.StationId,
            ChargerId = x.ChargerId,
            ReportedByUserId = x.ReportedByUserId,
            Title = x.Title,
            Description = x.Description,
            Severity = x.Severity,
            Status = x.Status,
            ReportedAt = x.ReportedAt,
            ResolvedAt = x.ResolvedAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
