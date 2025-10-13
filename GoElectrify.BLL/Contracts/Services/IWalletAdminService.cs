using GoElectrify.BLL.Dtos.Wallet;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IWalletAdminService
    {
        Task DepositManualAsync(int walletId, ManualDepositRequestDto dto);
    }
}
