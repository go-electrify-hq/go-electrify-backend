using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IWalletRepository
    {
        Task AddAsync(Wallet wallet, CancellationToken ct);
    }
}
