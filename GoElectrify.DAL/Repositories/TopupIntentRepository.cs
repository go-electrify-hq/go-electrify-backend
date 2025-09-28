using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class TopupIntentRepository(AppDbContext db) : ITopupIntentRepository
    {
        public Task AddAsync(TopupIntent intent, CancellationToken ct)
            => db.Set<TopupIntent>().AddAsync(intent, ct).AsTask();

        public Task<TopupIntent?> FindByProviderRefAsync(string provider, string providerRef, CancellationToken ct)
            => db.Set<TopupIntent>().FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderRef == providerRef, ct);

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
