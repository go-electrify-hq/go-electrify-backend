using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.Wallet;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public class WalletAdminService : IWalletAdminService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _txRepo;

        public WalletAdminService(IWalletRepository walletRepo, ITransactionRepository txRepo)
        {
            _walletRepo = walletRepo;
            _txRepo = txRepo;
        }

        public async Task DepositManualAsync(int userId, ManualDepositRequestDto dto)
        {
            // Lấy ví từ userId
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet is null)
                throw new InvalidOperationException($"Wallet not found for userId = {userId}");

            // Cộng tiền vào ví
            await _walletRepo.UpdateBalanceAsync(wallet.Id, dto.Amount);

            // Ghi transaction
            var tx = new Transaction
            {
                WalletId = wallet.Id,
                Amount = dto.Amount,
                Type = "DEPOSIT_MANUAL",
                Status = "SUCCEEDED",
                Note = dto.Note ?? "Nạp tiền thủ công"
            };

            await _txRepo.AddAsync(tx);
        }
    }
}
