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

        public async Task AddRangeAsync(IEnumerable<Transaction> items)
        {
            _db.Transactions.AddRange(items);
            await _db.SaveChangesAsync();
        }
        public async Task<bool> ExistsRefundByBookingIdAsync(int walletId, int bookingId, CancellationToken ct)
        {
            var tag = $"bookingId={bookingId}";
            return await _db.Transactions
                .AsNoTracking()
                .AnyAsync(t => t.WalletId == walletId
                            && t.Type == "REFUND"
                            && t.Note != null
                            && EF.Functions.ILike(t.Note, $"%{tag}%"), ct);
            // Nếu ILike chưa khả dụng ở provider hiện tại, thay bằng: t.Note!.Contains(tag)
        }

        public async Task<Transaction?> GetRefundByBookingIdAsync(int walletId, int bookingId, CancellationToken ct)
        {
            var tag = $"bookingId={bookingId}";
            return await _db.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId
                         && t.Type == "REFUND"
                         && t.Note != null
                         && EF.Functions.ILike(t.Note, $"%{tag}%"))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Transaction> CreateAsync(Transaction tx, CancellationToken ct)
        {
            _db.Transactions.Add(tx);
            await _db.SaveChangesAsync(ct);
            return tx;
        }
    }
}
