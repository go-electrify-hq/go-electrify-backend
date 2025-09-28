using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IExternalLoginRepository
    {
        Task<ExternalLogin?> FindAsync(string provider, string providerUserId, CancellationToken ct);
        Task AddAsync(ExternalLogin login, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
    }
}
