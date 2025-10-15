using GoElectrify.BLL.Dto.Subscription;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface ISubscriptionService
    {
        Task<IReadOnlyList<SubscriptionDto>> GetAllAsync(CancellationToken ct);
        Task<SubscriptionDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto dto, CancellationToken ct);
        Task<SubscriptionDto?> UpdateAsync(int id, SubscriptionUpdateDto dto, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);
    }
}
