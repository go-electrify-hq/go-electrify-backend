using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Wallet;
using GoElectrify.BLL.Dtos.WalletSubscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class WalletSubscriptionService : IWalletSubscriptionService
    {
        private readonly IWalletSubscriptionRepository _walletSubRepo;
        public WalletSubscriptionService(IWalletSubscriptionRepository walletSubRepo)
        {
            _walletSubRepo = walletSubRepo;
        }

        public async Task<PurchaseSubscriptionResponseDto> PurchaseAsync(
           int walletId, PurchaseSubscriptionRequestDto req, CancellationToken ct)
        {
            var startUtc = req.StartDate?.ToUniversalTime() ?? DateTime.UtcNow;

            var (ws, tx) = await _walletSubRepo.PurchaseSubscriptionAsync(
                walletId, req.SubscriptionId, startUtc, ct);

            var wsDto = new WalletSubscriptionDto
            {
                Id = ws.Id,
                WalletId = ws.WalletId,
                SubscriptionId = ws.SubscriptionId,
                Status = ws.Status,
                RemainingKwh = ws.RemainingKwh,
                StartDate = ws.StartDate,
                EndDate = ws.EndDate,
                CreatedAt = ws.CreatedAt
            };

            var txDto = new WalletTransactionDto
            {
                Id = tx.Id,
                WalletId = tx.WalletId,
                ChargingSessionId = tx.ChargingSessionId, // null
                Amount = tx.Amount,                        // số âm
                Type = tx.Type,                            // "SUBSCRIPTION"
                Status = tx.Status,                        // "SUCCEEDED"
                Note = tx.Note,                            // "Mua gói: {sub.Name}"
                CreatedAt = tx.CreatedAt
            };

            return new PurchaseSubscriptionResponseDto
            {
                WalletSubscription = wsDto,
                Transaction = txDto
            };
        }
    }
}
