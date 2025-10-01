using GoElectrify.BLL.Dto.Wallet;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface ITransactionService
    {
        Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsByWalletIdAsync(int walletId, DateTime? from = null, DateTime? to = null);
    }
}
