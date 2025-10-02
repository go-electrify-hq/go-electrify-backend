using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _db;
        public TransactionRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(int walletId, DateTime? from = null, DateTime? to = null)
        {
            var q = _db.Transactions
                       .AsNoTracking()
                       .Where(t => t.WalletId == walletId);

            if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);

            return await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }
    }
}
