using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IVehicleModelRepository
    {
        // CRUD cơ bản
        Task<List<VehicleModel>> ListAsync(string? search, CancellationToken ct);
        Task<VehicleModel?> GetByIdAsync(int id, CancellationToken ct);
        Task<VehicleModel?> GetDetailAsync(int id, CancellationToken ct);
        Task<bool> ExistsByNameAsync(string modelName, int? excludeId, CancellationToken ct);

        // Kiểm tra connector types hợp lệ
        Task<bool> AllConnectorTypesExistAsync(IEnumerable<int> ids, CancellationToken ct);

        // Thêm / xoá model
        Task AddAsync(VehicleModel entity, CancellationToken ct);
        void Remove(VehicleModel entity);

        // Xoá – thêm join (bảng VehicleModelConnectorTypes)
        Task RemoveAllJoinsAsync(int vehicleModelId, CancellationToken ct);
        Task AddJoinsAsync(int vehicleModelId, IEnumerable<int> connectorTypeIds, CancellationToken ct);

        //Xoá hàng loạt VehicleModel
        //Xóa nhiều VehicleModel theo danh sách Id, trả về số lượng đã xóa.
        Task<List<int>> FindIdsInBookingsAsync(IEnumerable<int> ids, CancellationToken ct);
        Task RemoveAllJoinsForManyAsync(IEnumerable<int> ids, CancellationToken ct);
        Task<int> DeleteManyAsync(IEnumerable<int> ids, CancellationToken ct);
        Task<int> DeleteManySafeAsync(IEnumerable<int> ids, CancellationToken ct); // gộp 2 DELETE trong 1 transaction

        Task SaveAsync(CancellationToken ct);
    }
}
