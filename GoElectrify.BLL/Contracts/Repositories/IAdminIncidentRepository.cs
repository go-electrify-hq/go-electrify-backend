using GoElectrify.BLL.Dto.Incidents;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IAdminIncidentRepository
    {
        // Tìm kiếm incidents theo filter. Nếu includeTotal=true → tính thêm tổng số record.
        Task<(List<AdminIncidentListItemDto> Items, int? Total)> SearchAsync(
            AdminIncidentListQueryDto query, bool includeTotal, CancellationToken ct);

        // Lấy 1 incident đã project sang DTO; trả null nếu không có.
        Task<AdminIncidentListItemDto?> GetProjectedByIdAsync(int incidentId, CancellationToken ct);
    }
}
