using GoElectrify.BLL.Dto.Incidents;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IAdminIncidentService
    {
        //Lấy danh sách Incident theo filter. 
        //includeTotal = true → service sẽ Count() để trả tổng, FE đọc từ header
        Task<(List<AdminIncidentListItemDto> Items, int? Total)> ListAsync(
            AdminIncidentListQueryDto query,
            bool includeTotal,
            CancellationToken ct);

        //Lấy chi tiết 1 Incident theo id. Ném KeyNotFoundException nếu không tồn tại.
        Task<AdminIncidentListItemDto> GetAsync(int incidentId, CancellationToken ct);
    }
}
