using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface ITopupIntentRepository
    {
        Task AddAsync(TopupIntent intent, CancellationToken ct);
        Task<TopupIntent?> FindByProviderRefAsync(string provider, string providerRef, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
    }
}
