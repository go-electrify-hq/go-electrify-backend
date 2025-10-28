using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories;

public interface IWalletRepository
{
    Task AddAsync(Wallet wallet, CancellationToken ct);
    Task<Wallet?> GetByIdAsync(int walletId);
    Task UpdateBalanceAsync(int walletId, decimal amount);
    Task UpdateAsync(int walletId, decimal amount);
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<string?> GetUserEmailByWalletAsync(int walletId);
}
