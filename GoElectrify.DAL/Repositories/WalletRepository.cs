using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;

namespace GoElectrify.DAL.Repositories
{
    public class WalletRepository(AppDbContext db) : IWalletRepository
    {
        public Task AddAsync(Wallet wallet, CancellationToken ct) => db.Wallets.AddAsync(wallet, ct).AsTask();
    }
}
