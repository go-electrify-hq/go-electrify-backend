using GoElectrify.BLL.Entities;

namespace GoElectrify.DAL.Repositories;

public interface ITopupIntentRepository
{
    Task<TopupIntent?> GetByProviderRefAsync(long orderCode);
    Task<TopupIntent> AddAsync(TopupIntent entity);
    Task UpdateAsync(TopupIntent entity);
}
