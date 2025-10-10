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

            if (from.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
                q = q.Where(t => t.CreatedAt >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
                q = q.Where(t => t.CreatedAt <= toUtc);
            }

            return await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }
        public async Task AddAsync(Transaction entity)
        {
            _db.Transactions.Add(entity);
            await _db.SaveChangesAsync();
        }
    }
}
