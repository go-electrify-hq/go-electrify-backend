using GoElectrify.BLL.Entities;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories;

public interface ITopupIntentRepository
{
    Task<TopupIntent?> GetByProviderRefAsync(long orderCode);
    Task<TopupIntent> AddAsync(TopupIntent entity);
    Task UpdateAsync(TopupIntent entity);
}
