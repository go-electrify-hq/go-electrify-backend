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
        IStationStaffRepository staffRepo,
        IIncidentRepository repo) : IIncidentService
    {
        public async Task<IncidentDto> CreateAsync(int stationId, int reporterUserId, IncidentCreateDto dto, CancellationToken ct)
        {
            // station & reporter phải hợp lệ
            var station = await stationRepo.GetByIdAsync(stationId);
            if (station == null) throw new KeyNotFoundException("Station not found.");

            // 2) User phải được assign (active) vào station
            var assignment = await staffRepo.GetAsync(stationId, reporterUserId, ct);
            if (assignment == null || assignment.RevokedAt != null)
                throw new InvalidOperationException("You are not assigned to this station.");

            var now = DateTime.UtcNow;

            var title = (dto.Title ?? string.Empty).Trim();
            if (title.Length > 128) title = title[..128];

            string? description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (description?.Length > 2048) description = description[..2048];

            var priority = (dto.Severity ?? "LOW").Trim().ToUpperInvariant(); // LOW|MEDIUM|HIGH|CRITICAL

            var entity = new Incident
            {
                StationId = stationId,
                ChargerId = dto.ChargerId,                  // nếu bảng có FK, có thể thêm check tồn tại sau
                ReportedByStationStaffId = assignment.Id,   
                Title = title,
                Description = description,
                Priority = priority,    // schema của bạn dùng Priority (string)
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

            var statusUpper = query.Status?.Trim().ToUpperInvariant();
            var priorityUpper = query.Severity?.Trim().ToUpperInvariant(); // DTO dùng "Severity", DB là "Priority"

            var list = await repo.ListByStationAsync(
                stationId,
                statusUpper,
                priorityUpper,
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

            // Chỉ staff đang active ở station được cập nhật
            var staff = await staffRepo.GetAsync(stationId, userId, ct);
            if (staff == null || staff.RevokedAt != null)
                throw new InvalidOperationException("You are not allowed to update this incident.");

            var newStatus = (dto.Status ?? string.Empty).Trim().ToUpperInvariant(); // OPEN|IN_PROGRESS|RESOLVED|CLOSED
            if (string.IsNullOrEmpty(newStatus))
                throw new ArgumentException("Status is required.");

            incident.Status = newStatus;

            if (newStatus is "RESOLVED" or "CLOSED")
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
            ReportedByStationStaffId = x.ReportedByStationStaffId ,
            Title = x.Title,
            Description = x.Description,
            Severity = x.Priority,
            Status = x.Status,
            ReportedAt = x.ReportedAt,
            ResolvedAt = x.ResolvedAt,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
