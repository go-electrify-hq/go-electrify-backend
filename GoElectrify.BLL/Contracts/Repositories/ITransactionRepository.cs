using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface ITransactionRepository
    {
        Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(int walletId, DateTime? from = null, DateTime? to = null);


    }
}
