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
        public async Task UpdateAsync(int walletId, decimal amount)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId);
            if (wallet == null)
                throw new Exception($"Wallet with id {walletId} not found");
            wallet.Balance = amount;
            await _db.SaveChangesAsync();
        }

        public Task AddAsync(Wallet wallet, CancellationToken ct) => _db.Wallets.AddAsync(wallet, ct).AsTask();
        public async Task<Wallet?> GetByUserIdAsync(int userId)
        {
            return await _db.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        
        public async Task<string?> GetUserEmailByWalletAsync(int walletId)
        {
            return await (from w in _db.Wallets.AsNoTracking()
                          join u in _db.Users.AsNoTracking() on w.UserId equals u.Id
                          where w.Id == walletId
                          select u.Email)
                         .FirstOrDefaultAsync();
        }
        public Task UpdateAsync(Wallet wallet, CancellationToken ct = default)
        {
            // Nếu wallet đang Detached thì attach & mark modified các field cần thiết
            var entry = _db.Entry(wallet);
            if (entry.State == EntityState.Detached)
            {
                _db.Wallets.Attach(wallet);
                entry.Property(x => x.Balance).IsModified = true;
                entry.Property(x => x.UpdatedAt).IsModified = true;
            }
            else
            {
                entry.Property(x => x.Balance).IsModified = true;
                entry.Property(x => x.UpdatedAt).IsModified = true;
            }
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}


