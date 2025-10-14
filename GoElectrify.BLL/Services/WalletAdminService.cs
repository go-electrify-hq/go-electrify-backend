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

        public async Task DepositManualAsync(int walletId, ManualDepositRequestDto dto)
        {
            var wallet = await _walletRepo.GetByIdAsync(walletId);
            if (wallet is null)
                throw new Exception("Wallet not found");
            if (dto.Amount < 10000)
                throw new Exception("Amount must be greater than or equal to 10.000");

            await _walletRepo.UpdateBalanceAsync(walletId, dto.Amount);

            var tx = new Transaction
            {
                WalletId = walletId,
                Amount = dto.Amount,
                Type = "DEPOSIT_MANUAL",
                Status = "SUCCEEDED",
                Note = dto.Note ?? "Manual deposit",
            };

            await _txRepo.AddAsync(tx);
        }
    }
}
