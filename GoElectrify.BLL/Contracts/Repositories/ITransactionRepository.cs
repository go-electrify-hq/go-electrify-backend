using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(int walletId, DateTime? from = null, DateTime? to = null);
    Task AddAsync(Transaction entity);

    Task AddRangeAsync(IEnumerable<Transaction> items);
    Task<bool> ExistsRefundByBookingIdAsync(int walletId, int bookingId, CancellationToken ct);
    Task<Transaction?> GetRefundByBookingIdAsync(int walletId, int bookingId, CancellationToken ct);
    Task<Transaction> CreateAsync(Transaction tx, CancellationToken ct);
}
