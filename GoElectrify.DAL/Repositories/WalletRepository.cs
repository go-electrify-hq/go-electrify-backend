using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _db;
        public WalletRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Wallet?> GetByIdAsync(int walletId)
        {
            return await _db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId);
        }

        public async Task UpdateBalanceAsync(int walletId, decimal amount)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId);
            if (wallet == null)
                throw new Exception($"Wallet with id {walletId} not found");

            wallet.Balance += amount;
            await _db.SaveChangesAsync();
        }

        public Task AddAsync(Wallet wallet, CancellationToken ct) => _db.Wallets.AddAsync(wallet, ct).AsTask();
        public async Task<Wallet?> GetByUserIdAsync(int userId)
        {
            return await _db.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }
    }
}


